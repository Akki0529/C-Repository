using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;

namespace ReservationService.Services;

// The first genuinely asynchronous, non-request-driven process in the system (per
// milestone-4): everything else runs because an HTTP request came in, but this runs
// on its own clock, hunting for Notified waitlist entries whose 48-hour claim window
// has silently passed.
public class WaitlistExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaitlistExpiryJob> _logger;
    private readonly TimeSpan _interval;

    public WaitlistExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<WaitlistExpiryJob> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var intervalSeconds = configuration.GetValue<int?>("WaitlistExpiryIntervalSeconds") ?? 3600;
        _interval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer (rather than Task.Delay in a loop) survives long-running ticks
        // without drifting, and disposes cleanly when the host shuts down.
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredEntriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // A single failed tick (e.g. CatalogService briefly unreachable) must not
                // kill the background service — it should keep trying on the next interval.
                _logger.LogError(ex, "Waitlist expiry job tick failed");
            }
        }
    }

    private async Task ProcessExpiredEntriesAsync(CancellationToken stoppingToken)
    {
        // BackgroundService is a singleton, but DbContext is scoped — a fresh scope is
        // required on every tick to get a valid, non-disposed DbContext instance.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReservationServiceContext>();
        var cascadeService = scope.ServiceProvider.GetRequiredService<IWaitlistCascadeService>();

        var now = DateTime.UtcNow;
        var expiredEntries = await context.WaitlistEntries
            .Where(w => w.Status == WaitlistStatus.Notified && w.ClaimDeadline != null && w.ClaimDeadline < now)
            .ToListAsync(stoppingToken);

        if (expiredEntries.Count == 0)
        {
            _logger.LogInformation("Waitlist expiry job: no expired claims found");
            return;
        }

        // Distinct book IDs up front: expiring the entry and cascading its book must
        // happen as two separate steps below, since the cascade re-queries WaitlistEntries
        // for that book and would otherwise still see this one as Notified.
        var bookIdsToCascade = new List<Guid>();

        foreach (var entry in expiredEntries)
        {
            entry.Status = WaitlistStatus.Expired;
            bookIdsToCascade.Add(entry.BookId);
        }

        await context.SaveChangesAsync(stoppingToken);

        foreach (var bookId in bookIdsToCascade)
        {
            await cascadeService.ReleaseOrCascadeAsync(bookId);
        }

        _logger.LogInformation(
            "Waitlist expiry job: expired {Count} claim(s) and ran cascade for {BookCount} book(s)",
            expiredEntries.Count, bookIdsToCascade.Distinct().Count());
    }
}

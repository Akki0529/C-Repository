using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;

namespace ReservationService.Services;

public class WaitlistCascadeService : IWaitlistCascadeService
{
    private readonly ReservationServiceContext _context;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly ILogger<WaitlistCascadeService> _logger;

    public WaitlistCascadeService(
        ReservationServiceContext context,
        ICatalogServiceClient catalogServiceClient,
        ILogger<WaitlistCascadeService> logger)
    {
        _context = context;
        _catalogServiceClient = catalogServiceClient;
        _logger = logger;
    }

    public async Task ReleaseOrCascadeAsync(Guid bookId)
    {
        // Walk the queue oldest-first. Each ineligible entry gets marked Expired and
        // skipped (per milestone-4: "expire that entry ... check the next entry"), so
        // this loop terminates once it finds an eligible patron or runs out of entries.
        while (true)
        {
            var nextInLine = await _context.WaitlistEntries
                .Where(w => w.BookId == bookId && w.Status == WaitlistStatus.Waiting)
                .OrderBy(w => w.JoinedAt)
                .FirstOrDefaultAsync();

            if (nextInLine is null)
            {
                // Queue is empty (or was exhausted by prior iterations of this loop) —
                // release the copy back to the general pool, same as the no-waitlist case.
                var result = await _catalogServiceClient.UpdateAvailabilityAsync(bookId, delta: 1);
                if (result is null)
                {
                    _logger.LogWarning(
                        "Failed to release availability for book {BookId} back to CatalogService after waitlist queue was empty/exhausted",
                        bookId);
                }
                return;
            }

            var activeReservationCount = await _context.Reservations
                .CountAsync(r => r.UserId == nextInLine.UserId &&
                    (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut));

            if (activeReservationCount >= ReservationRules.MaxActiveReservations)
            {
                // This patron aged out of eligibility between joining and their turn
                // arriving — expire their entry and move on to the next one in the queue.
                nextInLine.Status = WaitlistStatus.Expired;
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Waitlist entry {WaitlistId} for user {UserId} expired: at reservation limit ({Count}/{Max})",
                    nextInLine.WaitlistId, nextInLine.UserId, activeReservationCount, ReservationRules.MaxActiveReservations);
                continue;
            }

            // Eligible — the copy transfers directly to this patron. CatalogService's
            // availableCopies is intentionally left untouched: the copy never re-enters
            // the general pool, it just changes hands from "returned" to "claimed".
            var now = DateTime.UtcNow;
            var claimReservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                BookId = bookId,
                UserId = nextInLine.UserId,
                Status = ReservationStatus.Reserved,
                ReservedAt = now,
                ExpiresAt = now.AddDays(ReservationRules.ReservationExpiryDays),
                BookTitle = nextInLine.BookTitle,
                BookAuthor = nextInLine.BookAuthor
            };
            _context.Reservations.Add(claimReservation);

            nextInLine.Status = WaitlistStatus.Notified;
            nextInLine.NotifiedAt = now;
            nextInLine.ClaimDeadline = now.AddHours(ReservationRules.WaitlistClaimWindowHours);
            nextInLine.ResultingReservationId = claimReservation.ReservationId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Book {BookId} offered to waitlist entry {WaitlistId} (user {UserId}); claim deadline {ClaimDeadline}",
                bookId, nextInLine.WaitlistId, nextInLine.UserId, nextInLine.ClaimDeadline);
            return;
        }
    }
}

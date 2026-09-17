using Microsoft.EntityFrameworkCore;
using ReservationService.Models;

namespace ReservationService.Data;

public class ReservationServiceContext : DbContext
{
    public ReservationServiceContext(DbContextOptions<ReservationServiceContext> options) : base(options) { }

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.ReservationId);

            entity.Property(r => r.BookTitle).IsRequired().HasMaxLength(255);
            entity.Property(r => r.BookAuthor).IsRequired().HasMaxLength(255);

            // Store enums as strings for DB readability.
            entity.Property(r => r.Status).HasConversion<string>();
            entity.Property(r => r.Condition).HasConversion<string>();

            // Index on UserId speeds up "fetch all reservations for this user" queries,
            // which happen on every GET /api/reservations and GET /api/reservations/history call.
            entity.HasIndex(r => r.UserId);

            // Composite index for the most frequent active-reservations filter:
            // WHERE UserId = @id AND Status IN ('Reserved', 'CheckedOut')
            entity.HasIndex(r => new { r.UserId, r.Status });
        });

        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.HasKey(w => w.WaitlistId);

            entity.Property(w => w.BookTitle).IsRequired().HasMaxLength(255);
            entity.Property(w => w.BookAuthor).IsRequired().HasMaxLength(255);
            entity.Property(w => w.Status).HasConversion<string>();

            // Index supports "get all Waiting entries for this book ordered by JoinedAt"
            // which is the waitlist queue query triggered on every book return.
            entity.HasIndex(w => new { w.BookId, w.Status, w.JoinedAt });

            // Supports "get this user's active waitlist entries" for GET /api/reservations/waitlist.
            entity.HasIndex(w => new { w.UserId, w.Status });
        });
    }

    public override int SaveChanges()
    {
        ApplyAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            // Handle both entity types in one loop by checking for the audit properties.
            if (entry.State == EntityState.Added)
            {
                entry.Property("CreatedAt").CurrentValue = now;
                entry.Property("UpdatedAt").CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("CreatedAt").IsModified = false;
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}

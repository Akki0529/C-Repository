using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

public class UserServiceContext : DbContext
{
    public UserServiceContext(DbContextOptions<UserServiceContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);

            // Unique index on Email is the DB-level safety net for duplicate registrations.
            // The registration endpoint also checks first to return a clean 400 error, but
            // this index catches any race condition where two requests slip through simultaneously.
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);

            // Store enums as their string name so the PostgreSQL rows are human-readable
            // without needing a reference table (e.g. "Patron" instead of 0).
            entity.Property(u => u.Role).HasConversion<string>();
            entity.Property(u => u.MembershipStatus).HasConversion<string>();
        });
    }

    // Override both sync and async paths so audit timestamps are always set,
    // regardless of which SaveChanges variant the caller uses.
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

        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added)
            {
                // CreatedAt is set once at insert and never touched again.
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Prevent callers from accidentally overwriting CreatedAt on an update.
                entry.Property(u => u.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}

using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Data;

public class CatalogServiceContext : DbContext
{
    public CatalogServiceContext(DbContextOptions<CatalogServiceContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.BookId);

            // ISBN uniqueness prevents the same book being added twice under different IDs,
            // which would cause confusing split inventory.
            entity.HasIndex(b => b.Isbn).IsUnique();

            entity.Property(b => b.Isbn).IsRequired().HasMaxLength(20);
            entity.Property(b => b.Title).IsRequired().HasMaxLength(255);
            entity.Property(b => b.Author).IsRequired().HasMaxLength(255);
            entity.Property(b => b.Genre).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Language).HasMaxLength(50);

            // AvailableCopies must never go below 0; enforced in service logic but
            // noted here as a reminder that a check constraint could also live at the DB.
            entity.Property(b => b.AvailableCopies).HasDefaultValue(0);
            entity.Property(b => b.TotalCopies).HasDefaultValue(0);
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

        foreach (var entry in ChangeTracker.Entries<Book>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(b => b.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}

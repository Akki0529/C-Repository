using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CatalogService.Data;

// Design-time-only factory — see UserServiceContextFactory for the full rationale
// (the InMemory provider used at runtime can't generate migrations; this forces
// the EF CLI onto the Npgsql path regardless of appsettings.json's current value).
public class CatalogServiceContextFactory : IDesignTimeDbContextFactory<CatalogServiceContext>
{
    public CatalogServiceContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogServiceContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=CatalogServiceDb;Username=postgres;Password=placeholder");
        return new CatalogServiceContext(optionsBuilder.Options);
    }
}

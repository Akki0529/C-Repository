using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReservationService.Data;

// Design-time-only factory — see UserServiceContextFactory for the full rationale
// (the InMemory provider used at runtime can't generate migrations; this forces
// the EF CLI onto the Npgsql path regardless of appsettings.json's current value).
public class ReservationServiceContextFactory : IDesignTimeDbContextFactory<ReservationServiceContext>
{
    public ReservationServiceContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReservationServiceContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=ReservationServiceDb;Username=postgres;Password=placeholder");
        return new ReservationServiceContext(optionsBuilder.Options);
    }
}

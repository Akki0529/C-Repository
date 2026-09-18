using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserService.Data;

// Used only by `dotnet ef migrations add`/`database update` at design time.
// Program.cs picks InMemory vs Npgsql based on the "InMemory" sentinel in
// appsettings.json's DefaultConnection, but the InMemory provider has no
// migration support at all — so the EF CLI needs an explicit, always-Npgsql
// path to generate PostgreSQL-flavored migrations regardless of what dev's
// local appsettings.json currently says. The connection string below is never
// actually connected to; migrations add/generate C# code, it doesn't touch a
// database.
public class UserServiceContextFactory : IDesignTimeDbContextFactory<UserServiceContext>
{
    public UserServiceContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserServiceContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=UserServiceDb;Username=postgres;Password=placeholder");
        return new UserServiceContext(optionsBuilder.Options);
    }
}

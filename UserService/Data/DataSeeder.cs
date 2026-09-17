using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

public static class DataSeeder
{
    // Seed test accounts so the in-memory DB has users to work with on every startup.
    // Credentials:
    //   Patron:    john.doe@example.com    / Patron123!
    //   Librarian: librarian@library.com   / Librarian123!
    public static async Task SeedAsync(UserServiceContext context)
    {
        // Guard: skip seeding if rows already exist (e.g., on hot-reload restarts).
        if (await context.Users.AnyAsync())
            return;

        // Using fixed GUIDs makes it easy to reference these users in tests and Postman collections.
        var users = new List<User>
        {
            new()
            {
                UserId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                Email = "john.doe@example.com",
                // BCrypt.HashPassword runs the full hashing work-factor (default 11 rounds).
                // This is intentionally slow to resist brute-force attacks; that cost is
                // acceptable here at startup, not on every request.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Patron123!"),
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "+1-555-0123",
                Role = UserRole.Patron,
                MembershipStatus = MembershipStatus.Active,
                MemberSince = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                UserId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                Email = "librarian@library.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Librarian123!"),
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "+1-555-0199",
                Role = UserRole.Librarian,
                MembershipStatus = MembershipStatus.Active,
                MemberSince = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }
}

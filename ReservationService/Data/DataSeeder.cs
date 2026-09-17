namespace ReservationService.Data;

// Reservations are created entirely through the API (POST /api/reservations), so there
// is no pre-seeded data here. The seed data in UserService and CatalogService is
// sufficient to test the full reservation lifecycle from day one.
public static class DataSeeder
{
    public static Task SeedAsync(ReservationServiceContext context)
    {
        // No-op for now; reserved for future integration-test fixtures if needed.
        return Task.CompletedTask;
    }
}

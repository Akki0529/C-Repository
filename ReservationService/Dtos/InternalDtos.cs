namespace ReservationService.Dtos;

// Our own GET /api/reservations/statistics/{userId} response — consumed by
// UserService's /profile and /validate endpoints (see UserService's
// ReservationStatisticsDto, which this shape must stay in sync with).
public class ReservationStatisticsResponse
{
    public Guid UserId { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}

// Deserialized from UserService's GET /api/users/{userId}/validate response.
// Field names match that endpoint's JSON exactly (see api-contracts.md /
// milestone-2's "User Validation Flow" example).
public class UserValidationResult
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public int ActiveReservationsCount { get; set; }
}

// Deserialized from CatalogService's GET /api/catalog/books/{bookId} response.
// Only the fields ReservationService actually needs are declared — extra JSON
// properties (publisher, pageCount, etc.) are ignored by System.Text.Json by default.
public class BookDetailResult
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
}

// Deserialized from CatalogService's PUT /api/catalog/books/{bookId}/availability response.
public class AvailabilityUpdateResult
{
    public Guid BookId { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
}

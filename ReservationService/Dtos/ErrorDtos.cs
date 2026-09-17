namespace ReservationService.Dtos;

// Common error envelope — same shape as UserService/CatalogService's ApiErrorResponse,
// used for every error EXCEPT the three below that api-contracts.md defines with a
// business-context field in place of "timestamp".
public class ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiErrorResponse(string error, string message)
    {
        Error = error;
        Message = message;
    }
}

// POST /api/reservations, when the caller already has 5 active reservations.
// Deliberately has no "timestamp" field — api-contracts.md's example for this
// specific error carries "currentReservations" instead.
public class ReservationLimitExceededResponse
{
    public string Error { get; set; } = "RESERVATION_LIMIT_EXCEEDED";
    public string Message { get; set; } = string.Empty;
    public int CurrentReservations { get; set; }
}

// POST /api/reservations, when the target book has availableCopies = 0.
public class BookUnavailableResponse
{
    public string Error { get; set; } = "BOOK_UNAVAILABLE";
    public string Message { get; set; } = string.Empty;
    public int AvailableCopies { get; set; }
}

// Checkout/return, when the reservation isn't in the required status
// (Reserved for checkout, CheckedOut for return).
public class InvalidStatusResponse
{
    public string Error { get; set; } = "INVALID_STATUS";
    public string Message { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
}

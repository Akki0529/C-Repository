using ReservationService.Models;

namespace ReservationService.Dtos;

// POST /api/reservations request body.
public class CreateReservationRequest
{
    public Guid BookId { get; set; }
}

// POST /api/reservations success response (201).
public class ReservationResponse
{
    public Guid ReservationId { get; set; }
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Message { get; set; } = "Book reserved successfully. Please pick up within 7 days.";

    public static ReservationResponse FromReservation(Reservation r) => new()
    {
        ReservationId = r.ReservationId,
        BookId = r.BookId,
        UserId = r.UserId,
        BookTitle = r.BookTitle,
        Status = r.Status.ToWireString(),
        ReservedAt = r.ReservedAt,
        ExpiresAt = r.ExpiresAt
    };
}

// One row inside GET /api/reservations (active reservations only).
public class ActiveReservationDto
{
    public Guid ReservationId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Only one of each pair is populated, depending on Status — matches the contract's
    // example, which shows reservedAt/expiresAt/daysUntilExpiry for RESERVED rows and
    // checkedOutAt/dueDate/daysUntilDue for CHECKED_OUT rows.
    public DateTime? ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysUntilExpiry { get; set; }

    public DateTime? CheckedOutAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DaysUntilDue { get; set; }

    public static ActiveReservationDto FromReservation(Reservation r)
    {
        var dto = new ActiveReservationDto
        {
            ReservationId = r.ReservationId,
            BookId = r.BookId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            Status = r.Status.ToWireString()
        };

        if (r.Status == ReservationStatus.Reserved)
        {
            dto.ReservedAt = r.ReservedAt;
            dto.ExpiresAt = r.ExpiresAt;
            // Ceiling so "expires in a few hours" still reads as 1 day remaining, not 0.
            dto.DaysUntilExpiry = r.ExpiresAt.HasValue
                ? (int)Math.Ceiling((r.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                : null;
        }
        else if (r.Status == ReservationStatus.CheckedOut)
        {
            dto.CheckedOutAt = r.CheckedOutAt;
            dto.DueDate = r.DueDate;
            dto.DaysUntilDue = r.DueDate.HasValue
                ? (int)Math.Ceiling((r.DueDate.Value - DateTime.UtcNow).TotalDays)
                : null;
        }

        return dto;
    }
}

public class ActiveReservationsResponse
{
    public List<ActiveReservationDto> Reservations { get; set; } = new();
    public int TotalActive { get; set; }
}

// POST /api/reservations/{id}/checkout request/response.
public class CheckoutRequest
{
    public string? Notes { get; set; }
}

public class CheckoutResponse
{
    public Guid ReservationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedOutAt { get; set; }
    public DateTime DueDate { get; set; }
    public string Message { get; set; } = string.Empty;
}

// POST /api/reservations/{id}/return request/response.
public class ReturnRequest
{
    // Bound from the wire's uppercase "GOOD"/"FAIR"/"POOR"/"DAMAGED" (none of these enum
    // names contain underscores, so a case-insensitive JsonStringEnumConverter — registered
    // globally in Program.cs — is enough; no custom converter needed).
    public BookCondition Condition { get; set; }
    public string? Notes { get; set; }
}

public class ReturnResponse
{
    public Guid ReservationId { get; set; }
    public DateTime ReturnedAt { get; set; }
    // Only present on late returns, per the contract's two example shapes.
    public DateTime? DueDate { get; set; }
    public int LateDays { get; set; }
    public decimal LateFee { get; set; }
    public string Message { get; set; } = string.Empty;
}

// One row inside GET /api/reservations/history.
public class HistoryEntryDto
{
    public Guid ReservationId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool WasLate { get; set; }

    public static HistoryEntryDto FromReservation(Reservation r) => new()
    {
        ReservationId = r.ReservationId,
        BookTitle = r.BookTitle,
        BookAuthor = r.BookAuthor,
        ReservedAt = r.ReservedAt,
        CheckedOutAt = r.CheckedOutAt,
        ReturnedAt = r.ReturnedAt,
        DueDate = r.DueDate,
        Status = r.Status.ToWireString(),
        WasLate = r.ReturnedAt.HasValue && r.DueDate.HasValue && r.ReturnedAt.Value > r.DueDate.Value
    };
}

public class PagedHistoryResponse
{
    public List<HistoryEntryDto> Content { get; set; } = new();
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool Last { get; set; }
}

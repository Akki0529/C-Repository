using ReservationService.Models;

namespace ReservationService.Dtos;

// POST /api/reservations/waitlist request body.
public class JoinWaitlistRequest
{
    public Guid BookId { get; set; }
}

// Shape used both by POST /api/reservations/waitlist's 201 response and as one
// element of GET /api/reservations/waitlist's "entries" array. Position and
// claimDeadline are mutually exclusive on the wire (Waiting shows position,
// Notified shows claimDeadline), so both are nullable and the controller only
// populates the one that applies.
public class WaitlistEntryResponse
{
    public Guid WaitlistId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string? BookAuthor { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public int? Position { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ClaimDeadline { get; set; }

    public static WaitlistEntryResponse FromEntry(WaitlistEntry entry) => new()
    {
        WaitlistId = entry.WaitlistId,
        BookId = entry.BookId,
        BookTitle = entry.BookTitle,
        BookAuthor = entry.BookAuthor,
        Status = entry.Status.ToString().ToUpperInvariant(),
        JoinedAt = entry.JoinedAt,
        NotifiedAt = entry.NotifiedAt,
        ClaimDeadline = entry.ClaimDeadline
    };
}

public class WaitlistListResponse
{
    public List<WaitlistEntryResponse> Entries { get; set; } = new();
}

// DELETE /api/reservations/waitlist/{id} success response.
public class CancelWaitlistResponse
{
    public Guid WaitlistId { get; set; }
    public string Status { get; set; } = "CANCELLED";
    public string Message { get; set; } = "You have been removed from the waitlist";
}

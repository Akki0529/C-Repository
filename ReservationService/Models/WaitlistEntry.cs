using System.ComponentModel.DataAnnotations;

namespace ReservationService.Models;

// WaitlistEntry represents one patron's place in the queue for a fully-checked-out book.
// When a copy is returned and this entry is at the front of the eligible queue,
// the system auto-creates a Reservation and moves this entry to Notified.
public class WaitlistEntry
{
    public Guid WaitlistId { get; set; }

    // Plain Guids - no EF FK constraints across service boundaries.
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }

    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    // JoinedAt determines queue order; position is computed at query time, not stored.
    // Storing position would require renumbering on every cancellation or expiry.
    public DateTime JoinedAt { get; set; }

    // Set together when a copy is offered: NotifiedAt + 48 h = ClaimDeadline.
    public DateTime? NotifiedAt { get; set; }
    public DateTime? ClaimDeadline { get; set; }

    // Points to the auto-created Reservation once the patron is notified,
    // so the background job can track whether the claim was actually picked up.
    public Guid? ResultingReservationId { get; set; }

    // Cached from CatalogService at join time for the same reason as on Reservation.
    [Required]
    [MaxLength(255)]
    public string BookTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string BookAuthor { get; set; } = string.Empty;

    // Audit fields - auto-populated by ReservationServiceContext on every save.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

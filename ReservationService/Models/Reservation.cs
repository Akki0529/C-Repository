using System.ComponentModel.DataAnnotations;

namespace ReservationService.Models;

// Reservation represents the full borrowing lifecycle for one patron + one book copy.
// There are no EF navigation properties to User or Book because those entities live
// in separate services with separate databases. Cross-service data is either passed
// in the request or cached in BookTitle / BookAuthor below.
public class Reservation
{
    public Guid ReservationId { get; set; }

    // Foreign keys across service boundaries are plain Guids - no EF FK constraint.
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    // Timeline fields; each is set exactly once when the transition happens.
    public DateTime ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }     // ReservedAt + 7 days
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? DueDate { get; set; }        // CheckedOutAt + 14 days
    public DateTime? ReturnedAt { get; set; }

    // Late fee fields; only populated on return if returnedAt > dueDate.
    public int? LateDays { get; set; }
    public decimal? LateFee { get; set; }          // LateDays × $1.00

    // Condition is required at return time; null before return.
    public BookCondition? Condition { get; set; }

    // Optional free-text notes from the Librarian (checkout condition notes, etc.).
    public string? Notes { get; set; }

    // Cached from CatalogService at reservation time so history queries don't
    // need a cross-service call just to display a book title.
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

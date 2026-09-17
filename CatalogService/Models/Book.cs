using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models;

// Book is the only entity owned by CatalogService.
// ReservationService stores a cached copy of Title and Author in each
// Reservation row so it doesn't need to call CatalogService just to render history.
public class Book
{
    public Guid BookId { get; set; }

    // ISBN uniqueness is enforced at the DB index level (see CatalogServiceContext).
    [Required]
    [MaxLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Genre { get; set; } = string.Empty;

    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public int? PageCount { get; set; }

    [MaxLength(50)]
    public string? Language { get; set; }

    // TotalCopies = physical inventory count (never decremented).
    // AvailableCopies = TotalCopies minus currently reserved/checked-out copies.
    // Status (AVAILABLE / CHECKED_OUT) is derived from AvailableCopies at query time,
    // not stored, so it never goes stale.
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }

    // Audit fields - auto-populated by CatalogServiceContext on every save.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

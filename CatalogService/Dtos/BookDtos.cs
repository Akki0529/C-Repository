using CatalogService.Models;

namespace CatalogService.Dtos;

// Shared error envelope — identical shape to UserService's, per api-contracts.md's
// "Common Error Responses" section: { error, message, timestamp }.
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

// Row shape used in the GET /api/catalog/books listing — a summary, not full detail
// (no publisher/pageCount/language/audit fields; see BookDetailResponse for those).
public class BookSummaryDto
{
    public Guid BookId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;

    public static BookSummaryDto FromBook(Book book) => new()
    {
        BookId = book.BookId,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Genre = book.Genre,
        PublicationYear = book.PublicationYear,
        Description = book.Description,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        // Status is never stored — always derived from AvailableCopies at the moment
        // of the read, so it can never drift out of sync with the copy count.
        Status = book.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT"
    };
}

// Full detail shape for GET /api/catalog/books/{bookId} — adds publisher/pageCount/
// language/audit fields on top of the summary.
public class BookDetailResponse
{
    public Guid BookId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public int? PageCount { get; set; }
    public string? Language { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static BookDetailResponse FromBook(Book book) => new()
    {
        BookId = book.BookId,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Genre = book.Genre,
        PublicationYear = book.PublicationYear,
        Description = book.Description,
        Publisher = book.Publisher,
        PageCount = book.PageCount,
        Language = book.Language,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        Status = book.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT",
        CreatedAt = book.CreatedAt,
        UpdatedAt = book.UpdatedAt
    };
}

// Envelope for the paginated GET /api/catalog/books response.
public class PagedBooksResponse
{
    public List<BookSummaryDto> Content { get; set; } = new();
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool Last { get; set; }
}

// Request body for the internal PUT /api/catalog/books/{bookId}/availability endpoint.
// Not part of api-contracts.md's public contract — milestone-3 leaves the exact shape
// to our discretion ("you have flexibility"), since it's only ever called
// service-to-service by ReservationService, never by an end user.
// Delta is the signed change to apply: -1 when a reservation/checkout consumes a copy,
// +1 when a return releases one back (only when nobody is waiting on it — see
// milestone-3's Inventory Management note).
public class UpdateAvailabilityRequest
{
    public int Delta { get; set; }
}

public class UpdateAvailabilityResponse
{
    public Guid BookId { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
}

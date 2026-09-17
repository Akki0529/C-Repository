using CatalogService.Data;
using CatalogService.Dtos;
using CatalogService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly CatalogServiceContext _context;

    public CatalogController(CatalogServiceContext context)
    {
        _context = context;
    }

    // GET /api/catalog/books
    // All query parameters are optional; ASP.NET model-binds them from the query string
    // by name, applying the C# default values below when a parameter is omitted —
    // which is exactly the "page=0, size=20, sortBy=title, sortOrder=asc" default set
    // the contract specifies.
    [HttpGet("books")]
    public async Task<IActionResult> GetBooks(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        [FromQuery] string sortBy = "title",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? query = null,
        [FromQuery] string? genre = null,
        [FromQuery] string? isbn = null,
        [FromQuery] bool availableOnly = false)
    {
        // Guard against pathological input (negative page, zero/huge size) rather than
        // letting Skip/Take throw or return the entire table in one page.
        page = Math.Max(page, 0);
        size = Math.Clamp(size, 1, 100);

        IQueryable<Book> booksQuery = _context.Books.AsNoTracking();

        // "query" searches title OR author — a case-insensitive substring match.
        // ToLower()+Contains() (rather than EF.Functions.Like, which the InMemory
        // provider used in development can't translate) works identically on both
        // the InMemory provider and PostgreSQL in production.
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            booksQuery = booksQuery.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Author.ToLower().Contains(term));
        }

        // Genre and ISBN are exact matches per the contract, not substring searches.
        if (!string.IsNullOrWhiteSpace(genre))
        {
            booksQuery = booksQuery.Where(b => b.Genre == genre);
        }

        if (!string.IsNullOrWhiteSpace(isbn))
        {
            booksQuery = booksQuery.Where(b => b.Isbn == isbn);
        }

        if (availableOnly)
        {
            booksQuery = booksQuery.Where(b => b.AvailableCopies > 0);
        }

        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        // Whitelist sortBy against known columns instead of building a dynamic OrderBy
        // expression from arbitrary user input — keeps this immune to injection-style
        // property-name abuse and gives an unrecognized value a safe fallback (title).
        booksQuery = sortBy.ToLowerInvariant() switch
        {
            "author" => descending
                ? booksQuery.OrderByDescending(b => b.Author)
                : booksQuery.OrderBy(b => b.Author),
            "publicationyear" => descending
                ? booksQuery.OrderByDescending(b => b.PublicationYear)
                : booksQuery.OrderBy(b => b.PublicationYear),
            _ => descending
                ? booksQuery.OrderByDescending(b => b.Title)
                : booksQuery.OrderBy(b => b.Title)
        };

        var totalElements = await booksQuery.CountAsync();
        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling(totalElements / (double)size);

        var books = await booksQuery
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return Ok(new PagedBooksResponse
        {
            Content = books.Select(BookSummaryDto.FromBook).ToList(),
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            // "last" is true once this page reaches or exceeds the final page index —
            // using >= (not ==) so an out-of-range page number still reports last=true
            // instead of false, which would incorrectly invite the caller to page further.
            Last = page >= totalPages - 1
        });
    }

    // GET /api/catalog/books/{bookId}
    [HttpGet("books/{bookId:guid}")]
    public async Task<IActionResult> GetBookById(Guid bookId)
    {
        var book = await _context.Books.AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Book not found with ID: {bookId}"));
        }

        return Ok(BookDetailResponse.FromBook(book));
    }

    // PUT /api/catalog/books/{bookId}/availability  (internal — called by ReservationService
    // when a reservation is created, cancelled/expired, checked out, or returned; see
    // Dtos/BookDtos.cs for why this endpoint's shape isn't in api-contracts.md)
    [HttpPut("books/{bookId:guid}/availability")]
    public async Task<IActionResult> UpdateAvailability(Guid bookId, [FromBody] UpdateAvailabilityRequest request)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.BookId == bookId);
        if (book is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", $"Book not found with ID: {bookId}"));
        }

        var newAvailableCopies = book.AvailableCopies + request.Delta;

        // Clamp instead of trusting the caller's arithmetic: a bug in ReservationService's
        // cascade logic should never be able to push this negative or past physical inventory.
        if (newAvailableCopies < 0 || newAvailableCopies > book.TotalCopies)
        {
            return BadRequest(new ApiErrorResponse(
                "VALIDATION_ERROR",
                $"Resulting availableCopies ({newAvailableCopies}) would be outside the valid range [0, {book.TotalCopies}]"));
        }

        book.AvailableCopies = newAvailableCopies;
        await _context.SaveChangesAsync();

        return Ok(new UpdateAvailabilityResponse
        {
            BookId = book.BookId,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            Status = book.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT"
        });
    }
}

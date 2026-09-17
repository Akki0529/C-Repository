using ReservationService.Dtos;

namespace ReservationService.Services;

public interface ICatalogServiceClient
{
    // GET /api/catalog/books/{bookId}. Returns null if not found or CatalogService
    // is unreachable — callers treat both as "cannot verify this book right now".
    Task<BookDetailResult?> GetBookAsync(Guid bookId);

    // PUT /api/catalog/books/{bookId}/availability with the given signed delta
    // (-1 to consume a copy, +1 to release one). Returns null on failure so the
    // caller can decide whether to roll back its own already-applied change.
    Task<AvailabilityUpdateResult?> UpdateAvailabilityAsync(Guid bookId, int delta);
}

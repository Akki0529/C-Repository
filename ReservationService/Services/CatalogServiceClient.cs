using System.Net.Http.Json;
using ReservationService.Dtos;

namespace ReservationService.Services;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(IHttpClientFactory httpClientFactory, ILogger<CatalogServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<BookDetailResult?> GetBookAsync(Guid bookId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CatalogService");
            var response = await client.GetAsync($"/api/catalog/books/{bookId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CatalogService returned {StatusCode} fetching book {BookId}",
                    response.StatusCode, bookId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BookDetailResult>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "CatalogService unavailable while fetching book {BookId}", bookId);
            return null;
        }
    }

    public async Task<AvailabilityUpdateResult?> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("CatalogService");
            var response = await client.PutAsJsonAsync(
                $"/api/catalog/books/{bookId}/availability",
                new { delta });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CatalogService returned {StatusCode} updating availability for book {BookId} (delta {Delta})",
                    response.StatusCode, bookId, delta);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AvailabilityUpdateResult>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "CatalogService unavailable while updating availability for book {BookId}", bookId);
            return null;
        }
    }
}

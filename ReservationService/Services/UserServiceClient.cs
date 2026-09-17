using System.Net.Http.Json;
using ReservationService.Dtos;

namespace ReservationService.Services;

public class UserServiceClient : IUserServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(IHttpClientFactory httpClientFactory, ILogger<UserServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<UserValidationResult?> ValidateUserAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserService");
            var response = await client.GetAsync($"/api/users/{userId}/validate");

            // 404 (not found) and 400 (suspended) both mean "this user cannot reserve
            // right now" — the caller doesn't need to distinguish them, just that
            // validation failed.
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "UserService returned {StatusCode} validating user {UserId}",
                    response.StatusCode, userId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserValidationResult>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "UserService unavailable while validating user {UserId}", userId);
            return null;
        }
    }
}

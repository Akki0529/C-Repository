using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Dtos;
using UserService.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserServiceContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserServiceContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<UsersController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // GET /api/users/profile
    // [Authorize] rejects any request without a valid JWT before this method ever runs —
    // JwtBearerEvents in Program.cs turns that automatic rejection into the contract's
    // {error, message, timestamp} JSON shape instead of ASP.NET's default empty 401 body.
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // ClaimTypes.NameIdentifier is the claim JwtTokenService embedded as the userId
        // at login time (see Services/JwtTokenService.cs).
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("UNAUTHORIZED", "Authentication required"));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            // Token was valid but the user record is gone (e.g. deleted after the token
            // was issued). Treat it the same as "not authenticated".
            return Unauthorized(new ApiErrorResponse("UNAUTHORIZED", "Authentication required"));
        }

        var stats = await FetchReservationStatisticsAsync(userId);

        return Ok(new ProfileResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString().ToUpperInvariant(),
            MembershipStatus = user.MembershipStatus.ToString().ToUpperInvariant(),
            MemberSince = user.MemberSince,
            // Defaults to 0/0 when ReservationService is unreachable rather than failing
            // the whole profile request — per milestone-2: "handle Reservation Service
            // unavailability gracefully".
            ActiveReservations = stats?.ActiveReservations ?? 0,
            BorrowingHistory = stats?.BorrowingHistory ?? 0
        });
    }

    // GET /api/users/{userId}/validate  (internal — called by ReservationService, no auth
    // guard, since it's not exposed to end users through the public gateway/routing in this setup)
    [HttpGet("{userId:guid}/validate")]
    public async Task<IActionResult> ValidateUser(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound(new ApiErrorResponse("NOT_FOUND", "User not found"));
        }

        if (user.MembershipStatus != MembershipStatus.Active)
        {
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "User account is suspended"));
        }

        var stats = await FetchReservationStatisticsAsync(userId);

        return Ok(new ValidateUserResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString().ToUpperInvariant(),
            MembershipStatus = user.MembershipStatus.ToString().ToUpperInvariant(),
            ActiveReservationsCount = stats?.ActiveReservations ?? 0
        });
    }

    // Shared helper: calls ReservationService's internal statistics endpoint.
    // Returns null (rather than throwing) on any failure so callers can degrade gracefully
    // instead of taking down an otherwise-successful request over a downstream outage.
    private async Task<ReservationStatisticsDto?> FetchReservationStatisticsAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ReservationService");
            var response = await client.GetAsync($"/api/reservations/statistics/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ReservationService returned {StatusCode} for statistics/{UserId}",
                    response.StatusCode, userId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ReservationStatisticsDto>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // TaskCanceledException covers the HttpClient timeout (10s, set in Program.cs)
            // in addition to actual request cancellation.
            _logger.LogWarning(ex, "ReservationService unavailable while fetching statistics for {UserId}", userId);
            return null;
        }
    }
}

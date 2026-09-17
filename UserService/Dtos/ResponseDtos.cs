using UserService.Models;

namespace UserService.Dtos;

// Common error envelope shared by every endpoint per api-contracts.md's
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

// Response for POST /api/auth/register (201 Created).
public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = "Registration successful";

    // Builds the response straight from the persisted entity. The contract wants
    // "PATRON" / "ACTIVE" (uppercase), while the DB stores the enum's own casing
    // ("Patron" / "Active"), so we uppercase here rather than changing DB storage.
    public static RegisterResponse FromUser(User user) => new()
    {
        UserId = user.UserId,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role.ToString().ToUpperInvariant(),
        MembershipStatus = user.MembershipStatus.ToString().ToUpperInvariant(),
        CreatedAt = user.CreatedAt
    };
}

// The abbreviated user object nested inside the login response.
public class UserSummaryDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public static UserSummaryDto FromUser(User user) => new()
    {
        UserId = user.UserId,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role.ToString().ToUpperInvariant()
    };
}

// Response for POST /api/auth/login (200 OK).
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserSummaryDto User { get; set; } = new();
}

// Response for GET /api/users/profile (200 OK). Statistics come from ReservationService.
public class ProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public DateTime? MemberSince { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}

// Response for GET /api/users/{userId}/validate (200 OK) — internal, consumed by ReservationService.
public class ValidateUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public int ActiveReservationsCount { get; set; }
}

// Shape returned by ReservationService's internal GET /api/reservations/statistics/{userId}.
// Used both by the profile endpoint (activeReservations/borrowingHistory) and shared here
// since /validate only needs the active count.
public class ReservationStatisticsDto
{
    public Guid UserId { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}

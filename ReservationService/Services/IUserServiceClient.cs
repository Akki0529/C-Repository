using ReservationService.Dtos;

namespace ReservationService.Services;

public interface IUserServiceClient
{
    // Calls UserService's internal GET /api/users/{userId}/validate.
    // Returns null if the user doesn't exist, is suspended, or UserService is
    // unreachable — reservation creation treats all three as "cannot proceed"
    // (unlike UserService's own /profile endpoint, which degrades gracefully
    // when ReservationService is down; here the caller can't reserve a book
    // without knowing the user is valid, so there's no safe default to fall back to).
    Task<UserValidationResult?> ValidateUserAsync(Guid userId);
}

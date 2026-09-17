using UserService.Models;

namespace UserService.Services;

public interface IJwtTokenService
{
    // Builds a signed JWT for the given user. Returns the raw token string
    // (no "Bearer " prefix — that's added at the HTTP layer, not baked into the token).
    string GenerateToken(User user);
}

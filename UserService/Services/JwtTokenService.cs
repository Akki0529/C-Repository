using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserService.Models;

namespace UserService.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret must be set in appsettings.json");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiresInSeconds = int.Parse(jwtSection["ExpiresInSeconds"] ?? "86400");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // ClaimTypes.NameIdentifier / .Role are the standard ASP.NET Core claim types —
        // using them means ReservationService's [Authorize(Roles = "Librarian")] guards
        // (Phase 4) and User.FindFirstValue(ClaimTypes.NameIdentifier) calls work with zero
        // extra configuration on the validating side.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // Standard registered claims: iat records issue time as a Unix timestamp.
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            // "iat" is set manually above; "exp" is derived from this expiry.
            expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

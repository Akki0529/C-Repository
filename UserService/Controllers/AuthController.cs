using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Dtos;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserServiceContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserServiceContext context,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    // POST /api/auth/register
    // FluentValidation's auto-validation (wired in Program.cs) already ran RegisterRequestValidator
    // against the body before this action executes — field-level rules never reach this method.
    // What's left to check here is the one rule that needs a DB round-trip: email uniqueness.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
        {
            // 400, not 409 — the contract specifies VALIDATION_ERROR here, not a
            // dedicated conflict code, so duplicate email is treated as a validation failure.
            return BadRequest(new ApiErrorResponse("VALIDATION_ERROR", "Email already exists"));
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            // Role and MembershipStatus already default to Patron/Active on the model,
            // but setting them explicitly here documents the business rule at the call site.
            Role = UserRole.Patron,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(); // ApplyAuditFields() sets CreatedAt/UpdatedAt here.

        return StatusCode(StatusCodes.Status201Created, RegisterResponse.FromUser(user));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        // Deliberately vague: "invalid email or password" for both a missing user AND
        // a wrong password. Confirming "that email doesn't exist" to an attacker makes
        // account enumeration trivial.
        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ApiErrorResponse("AUTHENTICATION_FAILED", "Invalid email or password"));
        }

        var token = _jwtTokenService.GenerateToken(user);
        var expiresInSeconds = int.Parse(_configuration["Jwt:ExpiresInSeconds"] ?? "86400");

        return Ok(new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expiresInSeconds,
            User = UserSummaryDto.FromUser(user)
        });
    }
}

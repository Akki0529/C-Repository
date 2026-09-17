namespace UserService.Dtos;

// Request body for POST /api/auth/register. Field names match api-contracts.md exactly
// so System.Text.Json can bind them without any custom attributes.
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

// Request body for POST /api/auth/login.
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

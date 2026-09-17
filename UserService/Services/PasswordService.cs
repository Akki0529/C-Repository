namespace UserService.Services;

// Thin wrapper around BCrypt.Net so controllers never call the static BCrypt API
// directly — makes it swappable and mockable in unit tests.
public class PasswordService : IPasswordService
{
    // Work factor 11 (BCrypt's default) is deliberately slow — this is what makes
    // brute-forcing a stolen hash impractical. Do not lower it for "performance".
    public string HashPassword(string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 11);
    }

    public bool VerifyPassword(string plainTextPassword, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
    }
}

namespace UserService.Services;

public interface IPasswordService
{
    // Hashes a plaintext password for storage. Never store the raw password.
    string HashPassword(string plainTextPassword);

    // Compares a plaintext password against a stored BCrypt hash.
    bool VerifyPassword(string plainTextPassword, string passwordHash);
}

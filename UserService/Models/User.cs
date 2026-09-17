using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

// User is the only entity owned by UserService.
// Each other service stores userId as a plain Guid (no FK across service boundaries).
public class User
{
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    // Stored as a BCrypt hash; the raw password never touches the database.
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    // Defaults applied here keep the registration handler simple.
    public UserRole Role { get; set; } = UserRole.Patron;
    public MembershipStatus MembershipStatus { get; set; } = MembershipStatus.Active;

    // Nullable so that existing rows (before this field was added) remain valid.
    public DateTime? MemberSince { get; set; }

    // Audit fields - auto-populated by UserServiceContext on every save.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

using FluentValidation;
using UserService.Dtos;

namespace UserService.Validators;

// Rules mirror milestone-2's "Password Validation Requirements" and api-contracts.md
// exactly. Email uniqueness is NOT checked here — that requires a DB lookup, which the
// controller does separately so it can return the contract's specific "Email already
// exists" message instead of a generic validator message.
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]")
                .WithMessage("Password must contain at least one special character.");

        RuleFor(r => r.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(r => r.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        // Accepts formats like "+1-555-0123", "5550123", "(555) 012-3456".
        // Kept deliberately permissive: digits, spaces, and + - ( ) separators only.
        RuleFor(r => r.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20)
            .Matches(@"^\+?[0-9()\-\s]{7,20}$")
                .WithMessage("Phone number must be a valid phone number format.");
    }
}

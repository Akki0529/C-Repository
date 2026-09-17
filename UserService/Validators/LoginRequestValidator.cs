using FluentValidation;
using UserService.Dtos;

namespace UserService.Validators;

// Login only needs presence checks — password *strength* was already enforced at
// registration time, and re-checking it here would reject legitimate old passwords
// if the rules ever changed.
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

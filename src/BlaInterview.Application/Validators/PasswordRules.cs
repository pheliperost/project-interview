using FluentValidation;

namespace BlaInterview.Application.Validators;

internal static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must include at least one uppercase letter (A-Z).")
            .Matches("[a-z]").WithMessage("Password must include at least one lowercase letter (a-z).")
            .Matches("[0-9]").WithMessage("Password must include at least one number (0-9).")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must include at least one special character (e.g. ! @ #).");
}

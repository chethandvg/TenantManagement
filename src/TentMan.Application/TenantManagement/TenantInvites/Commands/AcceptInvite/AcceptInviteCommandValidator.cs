using FluentValidation;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.AcceptInvite;

/// <summary>
/// Validator for AcceptInviteCommand.
/// </summary>
public sealed class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteCommandValidator()
    {
        RuleFor(x => x.InviteToken)
            .NotEmpty().WithMessage("Invite token is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character (@$!%*?&#)");
    }
}

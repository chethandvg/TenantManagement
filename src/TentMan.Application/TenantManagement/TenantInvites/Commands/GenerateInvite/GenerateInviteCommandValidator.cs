using FluentValidation;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.GenerateInvite;

/// <summary>
/// Validator for GenerateInviteCommand.
/// </summary>
public sealed class GenerateInviteCommandValidator : AbstractValidator<GenerateInviteCommand>
{
    public GenerateInviteCommandValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty().WithMessage("Organization ID is required");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.ExpiryDays)
            .GreaterThanOrEqualTo(1).WithMessage("Expiry days must be at least 1 day")
            .LessThanOrEqualTo(90).WithMessage("Expiry days must not exceed 90 days");
    }
}

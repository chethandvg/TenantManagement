using FluentValidation;

namespace TentMan.Application.TenantManagement.TenantInvites.Queries.ValidateInvite;

/// <summary>
/// Validator for ValidateInviteQuery.
/// </summary>
public sealed class ValidateInviteQueryValidator : AbstractValidator<ValidateInviteQuery>
{
    public ValidateInviteQueryValidator()
    {
        RuleFor(x => x.InviteToken)
            .NotEmpty().WithMessage("Invite token is required");
    }
}

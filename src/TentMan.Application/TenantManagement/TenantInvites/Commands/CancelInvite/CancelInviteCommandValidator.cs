using FluentValidation;

namespace TentMan.Application.TenantManagement.TenantInvites.Commands.CancelInvite;

public class CancelInviteCommandValidator : AbstractValidator<CancelInviteCommand>
{
    public CancelInviteCommandValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("Organization ID is required");

        RuleFor(x => x.InviteId)
            .NotEmpty()
            .WithMessage("Invite ID is required");
    }
}

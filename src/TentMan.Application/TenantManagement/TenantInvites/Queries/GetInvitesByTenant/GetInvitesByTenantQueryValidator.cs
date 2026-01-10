using FluentValidation;

namespace TentMan.Application.TenantManagement.TenantInvites.Queries.GetInvitesByTenant;

public class GetInvitesByTenantQueryValidator : AbstractValidator<GetInvitesByTenantQuery>
{
    public GetInvitesByTenantQueryValidator()
    {
        RuleFor(x => x.OrgId)
            .NotEmpty()
            .WithMessage("Organization ID is required");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");
    }
}

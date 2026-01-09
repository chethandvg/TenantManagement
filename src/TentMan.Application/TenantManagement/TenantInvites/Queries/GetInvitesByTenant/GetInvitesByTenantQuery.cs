using TentMan.Contracts.TenantInvites;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantInvites.Queries.GetInvitesByTenant;

public record GetInvitesByTenantQuery(
    Guid OrgId,
    Guid TenantId
) : IRequest<IEnumerable<TenantInviteDto>>;

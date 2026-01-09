using TentMan.Contracts.Tenants;
using MediatR;

namespace TentMan.Application.TenantManagement.Tenants.Queries;

public record GetTenantsQuery(
    Guid OrgId,
    string? Search = null
) : IRequest<IEnumerable<TenantListDto>>;

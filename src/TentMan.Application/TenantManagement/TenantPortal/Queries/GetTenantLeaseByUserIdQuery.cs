using TentMan.Contracts.TenantPortal;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantPortal.Queries;

/// <summary>
/// Query to get the active lease for a tenant by their user ID.
/// </summary>
public record GetTenantLeaseByUserIdQuery(Guid UserId) : IRequest<TenantLeaseSummaryResponse?>;

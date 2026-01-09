using TentMan.Contracts.Leases;
using MediatR;

namespace TentMan.Application.TenantManagement.Leases.Queries;

public record GetLeasesByUnitQuery(Guid UnitId) : IRequest<IEnumerable<LeaseListDto>>;

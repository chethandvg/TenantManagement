using TentMan.Contracts.Leases;
using MediatR;

namespace TentMan.Application.TenantManagement.Leases.Queries;

public record GetLeaseByIdQuery(Guid LeaseId) : IRequest<LeaseDetailDto?>;

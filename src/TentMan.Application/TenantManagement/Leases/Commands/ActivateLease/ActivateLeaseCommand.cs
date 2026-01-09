using TentMan.Contracts.Leases;
using MediatR;

namespace TentMan.Application.TenantManagement.Leases.Commands.ActivateLease;

public record ActivateLeaseCommand(
    Guid LeaseId,
    byte[] RowVersion
) : IRequest<LeaseDetailDto>;

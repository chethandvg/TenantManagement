using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.TenantManagement.Leases.Commands.AddLeaseParty;

public record AddLeasePartyCommand(
    Guid LeaseId,
    Guid TenantId,
    LeasePartyRole Role,
    bool IsResponsibleForPayment,
    DateOnly? MoveInDate
) : IRequest<LeaseDetailDto>;

using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.TenantManagement.Leases.Commands.AddLeaseTerm;

public record AddLeaseTermCommand(
    Guid LeaseId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal MonthlyRent,
    decimal SecurityDeposit,
    decimal? MaintenanceCharge,
    decimal? OtherFixedCharge,
    EscalationType EscalationType,
    decimal? EscalationValue,
    short? EscalationEveryMonths,
    string? Notes
) : IRequest<LeaseDetailDto>;

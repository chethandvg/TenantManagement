using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.TenantManagement.Leases.Commands.CreateLease;

public record CreateLeaseCommand(
    Guid OrgId,
    Guid UnitId,
    string? LeaseNumber,
    DateOnly StartDate,
    DateOnly? EndDate,
    byte RentDueDay,
    byte GraceDays,
    short? NoticePeriodDays,
    LateFeeType LateFeeType,
    decimal? LateFeeValue,
    string? PaymentMethodNote,
    string? TermsText,
    bool IsAutoRenew
) : IRequest<LeaseListDto>;

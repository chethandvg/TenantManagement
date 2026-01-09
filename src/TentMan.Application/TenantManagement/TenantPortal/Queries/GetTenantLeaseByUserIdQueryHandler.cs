using TentMan.Application.Abstractions;
using TentMan.Contracts.Enums;
using TentMan.Contracts.TenantPortal;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantPortal.Queries;

public class GetTenantLeaseByUserIdQueryHandler : IRequestHandler<GetTenantLeaseByUserIdQuery, TenantLeaseSummaryResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantLeaseByUserIdQueryHandler> _logger;

    public GetTenantLeaseByUserIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantLeaseByUserIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TenantLeaseSummaryResponse?> Handle(GetTenantLeaseByUserIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting active lease for user: {UserId}", request.UserId);

        // Get tenant by linked user ID with lease parties
        var tenant = await _unitOfWork.Tenants.GetByLinkedUserIdAsync(request.UserId, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for user: {UserId}", request.UserId);
            return null;
        }

        // Find the active lease for this tenant
        var activeLease = tenant.LeaseParties
            .Where(lp => !lp.IsDeleted && 
                   lp.Lease != null && 
                   (lp.Lease.Status == LeaseStatus.Active || lp.Lease.Status == LeaseStatus.NoticeGiven))
            .Select(lp => lp.Lease)
            .FirstOrDefault();

        if (activeLease == null)
        {
            _logger.LogInformation("No active lease found for user: {UserId}", request.UserId);
            return null;
        }

        return MapToTenantLeaseSummaryResponse(activeLease, tenant.Id);
    }

    private static TenantLeaseSummaryResponse MapToTenantLeaseSummaryResponse(Lease lease, Guid currentTenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeTerm = lease.Terms
            .Where(t => !t.IsDeleted &&
                        t.EffectiveFrom <= today &&
                        (t.EffectiveTo == null || t.EffectiveTo >= today))
            .OrderByDescending(t => t.EffectiveFrom)
            .FirstOrDefault();

        var totalCollected = lease.DepositTransactions
            .Where(d => !d.IsDeleted && d.TxnType == DepositTransactionType.Collected)
            .Sum(d => d.Amount);
        
        // Note: DepositTransactionType.Adjustment transactions are intentionally excluded
        // from deposit balance calculations. Adjustments affect the deposit amount directly
        // and are already reflected in the current deposit value, not as separate refunds/deductions.
        var totalRefunded = lease.DepositTransactions
            .Where(d => !d.IsDeleted && (d.TxnType == DepositTransactionType.Refund || d.TxnType == DepositTransactionType.Deduction))
            .Sum(d => d.Amount);

        return new TenantLeaseSummaryResponse
        {
            LeaseId = lease.Id,
            LeaseNumber = lease.LeaseNumber,
            Status = lease.Status,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            RentDueDay = lease.RentDueDay,
            GraceDays = lease.GraceDays,
            NoticePeriodDays = lease.NoticePeriodDays,
            IsAutoRenew = lease.IsAutoRenew,
            TermsText = lease.TermsText,
            PaymentMethodNote = lease.PaymentMethodNote,
            LateFeeType = lease.LateFeeType,
            LateFeeValue = lease.LateFeeValue,
            UnitId = lease.UnitId,
            UnitNumber = lease.Unit?.UnitNumber,
            BuildingName = lease.Unit?.Building?.Name,
            BuildingAddress = FormatBuildingAddress(lease.Unit?.Building?.Address),
            MonthlyRent = activeTerm?.MonthlyRent ?? 0,
            SecurityDeposit = activeTerm?.SecurityDeposit ?? 0,
            MaintenanceCharge = activeTerm?.MaintenanceCharge,
            OtherFixedCharge = activeTerm?.OtherFixedCharge,
            TotalDepositCollected = totalCollected,
            DepositBalance = totalCollected - totalRefunded,
            LeaseParties = lease.Parties
                .Where(p => !p.IsDeleted && p.TenantId != currentTenantId)
                .Select(p => new TenantLeasePartyDto
                {
                    FullName = p.Tenant?.FullName ?? "",
                    Role = p.Role,
                    IsResponsibleForPayment = p.IsResponsibleForPayment,
                    MoveInDate = p.MoveInDate,
                    MoveOutDate = p.MoveOutDate
                }).ToList(),
            TermsHistory = lease.Terms
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.EffectiveFrom)
                .Select(t => new TenantLeaseTermDto
                {
                    EffectiveFrom = t.EffectiveFrom,
                    EffectiveTo = t.EffectiveTo,
                    MonthlyRent = t.MonthlyRent,
                    SecurityDeposit = t.SecurityDeposit,
                    MaintenanceCharge = t.MaintenanceCharge,
                    OtherFixedCharge = t.OtherFixedCharge,
                    IsActive = t == activeTerm
                }).ToList(),
            DepositTransactions = lease.DepositTransactions
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.TxnDate)
                .Select(d => new TenantDepositTransactionDto
                {
                    TxnType = d.TxnType,
                    Amount = d.Amount,
                    TxnDate = d.TxnDate,
                    Reference = d.Reference,
                    Notes = d.Notes
                }).ToList()
        };
    }

    private static string? FormatBuildingAddress(BuildingAddress? address)
    {
        if (address == null)
            return null;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.Line1))
            parts.Add(address.Line1);
        if (!string.IsNullOrWhiteSpace(address.Line2))
            parts.Add(address.Line2);
        if (!string.IsNullOrWhiteSpace(address.City))
            parts.Add(address.City);
        if (!string.IsNullOrWhiteSpace(address.State))
            parts.Add(address.State);
        if (!string.IsNullOrWhiteSpace(address.PostalCode))
            parts.Add(address.PostalCode);

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }
}

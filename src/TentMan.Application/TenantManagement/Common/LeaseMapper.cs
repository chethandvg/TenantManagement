using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.TenantManagement.Common;

/// <summary>
/// Shared mapper for Lease entity to LeaseDetailDto.
/// </summary>
public static class LeaseMapper
{
    public static LeaseDetailDto ToDetailDto(Lease lease)
    {
        var activeTerm = lease.Terms
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.EffectiveFrom)
            .FirstOrDefault(t => t.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow) && 
                                 (t.EffectiveTo == null || t.EffectiveTo >= DateOnly.FromDateTime(DateTime.UtcNow)));

        var totalCollected = lease.DepositTransactions
            .Where(d => !d.IsDeleted && d.TxnType == DepositTransactionType.Collected)
            .Sum(d => d.Amount);
        var totalRefunded = lease.DepositTransactions
            .Where(d => !d.IsDeleted && (d.TxnType == DepositTransactionType.Refund || d.TxnType == DepositTransactionType.Deduction))
            .Sum(d => d.Amount);

        return new LeaseDetailDto
        {
            Id = lease.Id,
            OrgId = lease.OrgId,
            UnitId = lease.UnitId,
            UnitNumber = lease.Unit?.UnitNumber,
            BuildingName = lease.Unit?.Building?.Name,
            LeaseNumber = lease.LeaseNumber,
            Status = lease.Status,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            RentDueDay = lease.RentDueDay,
            GraceDays = lease.GraceDays,
            NoticePeriodDays = lease.NoticePeriodDays,
            LateFeeType = lease.LateFeeType,
            LateFeeValue = lease.LateFeeValue,
            PaymentMethodNote = lease.PaymentMethodNote,
            TermsText = lease.TermsText,
            IsAutoRenew = lease.IsAutoRenew,
            CreatedAtUtc = lease.CreatedAtUtc,
            ModifiedAtUtc = lease.ModifiedAtUtc,
            RowVersion = lease.RowVersion,
            TotalDepositCollected = totalCollected,
            DepositBalance = totalCollected - totalRefunded,
            Parties = lease.Parties.Where(p => !p.IsDeleted).Select(p => new LeasePartyDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                TenantName = p.Tenant?.FullName ?? "",
                TenantPhone = p.Tenant?.Phone ?? "",
                Role = p.Role,
                IsResponsibleForPayment = p.IsResponsibleForPayment,
                MoveInDate = p.MoveInDate,
                MoveOutDate = p.MoveOutDate
            }).ToList(),
            Terms = lease.Terms.Where(t => !t.IsDeleted).OrderByDescending(t => t.EffectiveFrom).Select(t => new LeaseTermDto
            {
                Id = t.Id,
                EffectiveFrom = t.EffectiveFrom,
                EffectiveTo = t.EffectiveTo,
                MonthlyRent = t.MonthlyRent,
                SecurityDeposit = t.SecurityDeposit,
                MaintenanceCharge = t.MaintenanceCharge,
                OtherFixedCharge = t.OtherFixedCharge,
                EscalationType = t.EscalationType,
                EscalationValue = t.EscalationValue,
                EscalationEveryMonths = t.EscalationEveryMonths,
                Notes = t.Notes,
                IsActive = t == activeTerm
            }).ToList(),
            DepositTransactions = lease.DepositTransactions.Where(d => !d.IsDeleted).OrderByDescending(d => d.TxnDate).Select(d => new DepositTransactionDto
            {
                Id = d.Id,
                TxnType = d.TxnType,
                Amount = d.Amount,
                TxnDate = d.TxnDate,
                Reference = d.Reference,
                Notes = d.Notes
            }).ToList()
        };
    }
}

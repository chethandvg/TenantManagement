using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Leases.Commands.AddLeaseParty;

public class AddLeasePartyCommandHandler : BaseCommandHandler, IRequestHandler<AddLeasePartyCommand, LeaseDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddLeasePartyCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AddLeasePartyCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LeaseDetailDto> Handle(AddLeasePartyCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Adding party {TenantId} to lease {LeaseId}", request.TenantId, request.LeaseId);

        var lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken)
            ?? throw new InvalidOperationException($"Lease {request.LeaseId} not found");

        // Validate tenant exists
        if (!await _unitOfWork.Tenants.ExistsAsync(request.TenantId, cancellationToken))
        {
            throw new InvalidOperationException($"Tenant {request.TenantId} not found");
        }

        // Check if tenant is already in the lease
        if (lease.Parties.Any(p => p.TenantId == request.TenantId && !p.IsDeleted))
        {
            throw new InvalidOperationException($"Tenant {request.TenantId} is already a party in this lease");
        }

        var leaseParty = new LeaseParty
        {
            LeaseId = request.LeaseId,
            TenantId = request.TenantId,
            Role = request.Role,
            IsResponsibleForPayment = request.IsResponsibleForPayment,
            MoveInDate = request.MoveInDate
        };

        lease.Parties.Add(leaseParty);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Party added to lease {LeaseId}", request.LeaseId);

        // Reload to get updated data
        lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken);
        return MapToDetailDto(lease!);
    }

    private static LeaseDetailDto MapToDetailDto(Lease lease)
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

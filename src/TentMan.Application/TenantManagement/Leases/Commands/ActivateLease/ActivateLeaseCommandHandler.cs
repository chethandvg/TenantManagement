using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Leases.Commands.ActivateLease;

public class ActivateLeaseCommandHandler : BaseCommandHandler, IRequestHandler<ActivateLeaseCommand, LeaseDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivateLeaseCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ActivateLeaseCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LeaseDetailDto> Handle(ActivateLeaseCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Activating lease {LeaseId}", request.LeaseId);

        return await _unitOfWork.ExecuteWithRetryAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken)
                    ?? throw new InvalidOperationException($"Lease {request.LeaseId} not found");

                // Validate lease is in Draft status
                if (lease.Status != LeaseStatus.Draft)
                {
                    throw new InvalidOperationException($"Lease can only be activated from Draft status. Current status: {lease.Status}");
                }

                // Validate unit has no other active/notice lease
                if (await _unitOfWork.Leases.HasActiveLeaseAsync(lease.UnitId, lease.Id, cancellationToken))
                {
                    throw new InvalidOperationException("This unit already has an active lease. Only one active lease per unit is allowed.");
                }

                // Validate StartDate is valid
                // (Already done in CreateLease, but double check)

                // Validate EndDate > StartDate if exists
                if (lease.EndDate.HasValue && lease.EndDate.Value <= lease.StartDate)
                {
                    throw new InvalidOperationException("EndDate must be after StartDate");
                }

                // Validate at least one LeaseParty marked PrimaryTenant
                var hasPrimaryTenant = lease.Parties.Any(p => !p.IsDeleted && p.Role == LeasePartyRole.PrimaryTenant);
                if (!hasPrimaryTenant)
                {
                    throw new InvalidOperationException("Lease must have at least one Primary Tenant");
                }

                // Validate at least one LeaseTerm covering StartDate
                var hasTermCoveringStart = lease.Terms.Any(t => !t.IsDeleted && 
                    t.EffectiveFrom <= lease.StartDate && 
                    (t.EffectiveTo == null || t.EffectiveTo >= lease.StartDate));
                if (!hasTermCoveringStart)
                {
                    throw new InvalidOperationException("Lease must have at least one term covering the start date");
                }

                // Validate RentDueDay in 1-28
                if (lease.RentDueDay < 1 || lease.RentDueDay > 28)
                {
                    throw new InvalidOperationException("RentDueDay must be between 1 and 28");
                }

                // Activate the lease
                lease.Status = LeaseStatus.Active;

                // Create unit occupancy record
                var occupancy = new UnitOccupancy
                {
                    UnitId = lease.UnitId,
                    LeaseId = lease.Id,
                    FromDate = lease.StartDate,
                    ToDate = lease.EndDate,
                    Status = UnitOccupancyHistoryStatus.Occupied
                };
                lease.Occupancies.Add(occupancy);

                // Update unit occupancy status
                var unit = await _unitOfWork.Units.GetByIdAsync(lease.UnitId, cancellationToken);
                if (unit != null)
                {
                    unit.OccupancyStatus = OccupancyStatus.Occupied;
                }

                await _unitOfWork.Leases.UpdateAsync(lease, request.RowVersion, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                Logger.LogInformation("Lease {LeaseId} activated successfully", request.LeaseId);

                // Reload to get updated data
                lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken);
                return MapToDetailDto(lease!);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }, cancellationToken);
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

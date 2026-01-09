using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Application.TenantManagement.Common;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Leases.Commands.AddLeaseTerm;

public class AddLeaseTermCommandHandler : BaseCommandHandler, IRequestHandler<AddLeaseTermCommand, LeaseDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddLeaseTermCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AddLeaseTermCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LeaseDetailDto> Handle(AddLeaseTermCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Adding term to lease {LeaseId} effective from {EffectiveFrom}", 
            request.LeaseId, request.EffectiveFrom);

        var lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken)
            ?? throw new InvalidOperationException($"Lease {request.LeaseId} not found");

        // Validate EffectiveTo > EffectiveFrom if EffectiveTo is provided
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value <= request.EffectiveFrom)
        {
            throw new InvalidOperationException("EffectiveTo must be after EffectiveFrom");
        }

        // Check for duplicate effective date - enforce at most one term per lease per effective date
        var existingTerm = lease.Terms.FirstOrDefault(t => t.EffectiveFrom == request.EffectiveFrom);
        if (existingTerm != null)
        {
            throw new InvalidOperationException(
                $"A term with effective date {request.EffectiveFrom:yyyy-MM-dd} already exists for this lease. " +
                "Each lease can only have one term per effective date.");
        }

        var leaseTerm = new LeaseTerm
        {
            LeaseId = request.LeaseId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit,
            MaintenanceCharge = request.MaintenanceCharge,
            OtherFixedCharge = request.OtherFixedCharge,
            EscalationType = request.EscalationType,
            EscalationValue = request.EscalationValue,
            EscalationEveryMonths = request.EscalationEveryMonths,
            Notes = request.Notes
        };

        lease.Terms.Add(leaseTerm);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Term added to lease {LeaseId}", request.LeaseId);

        // Reload to get updated data
        lease = await _unitOfWork.Leases.GetByIdWithDetailsAsync(request.LeaseId, cancellationToken);
        return LeaseMapper.ToDetailDto(lease!);
    }
}

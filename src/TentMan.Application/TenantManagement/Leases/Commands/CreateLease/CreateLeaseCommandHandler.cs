using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Leases;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Leases.Commands.CreateLease;

public class CreateLeaseCommandHandler : BaseCommandHandler, IRequestHandler<CreateLeaseCommand, LeaseListDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateLeaseCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateLeaseCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LeaseListDto> Handle(CreateLeaseCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating lease for unit: {UnitId} in org: {OrgId}", request.UnitId, request.OrgId);

        // Validate organization exists
        if (!await _unitOfWork.Organizations.ExistsAsync(request.OrgId, cancellationToken))
        {
            throw new InvalidOperationException($"Organization {request.OrgId} not found");
        }

        // Validate unit exists
        if (!await _unitOfWork.Units.ExistsAsync(request.UnitId, cancellationToken))
        {
            throw new InvalidOperationException($"Unit {request.UnitId} not found");
        }

        // Validate RentDueDay is between 1 and 28
        if (request.RentDueDay < 1 || request.RentDueDay > 28)
        {
            throw new InvalidOperationException("RentDueDay must be between 1 and 28");
        }

        // Validate EndDate > StartDate if EndDate is provided
        if (request.EndDate.HasValue && request.EndDate.Value <= request.StartDate)
        {
            throw new InvalidOperationException("EndDate must be after StartDate");
        }

        var lease = new Lease
        {
            OrgId = request.OrgId,
            UnitId = request.UnitId,
            LeaseNumber = request.LeaseNumber,
            Status = LeaseStatus.Draft, // Always start as draft
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RentDueDay = request.RentDueDay,
            GraceDays = request.GraceDays,
            NoticePeriodDays = request.NoticePeriodDays,
            LateFeeType = request.LateFeeType,
            LateFeeValue = request.LateFeeValue,
            PaymentMethodNote = request.PaymentMethodNote,
            TermsText = request.TermsText,
            IsAutoRenew = request.IsAutoRenew
        };

        await _unitOfWork.Leases.AddAsync(lease, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Lease created with ID: {LeaseId}", lease.Id);

        return new LeaseListDto
        {
            Id = lease.Id,
            UnitId = lease.UnitId,
            LeaseNumber = lease.LeaseNumber,
            Status = lease.Status,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            RowVersion = lease.RowVersion
        };
    }
}

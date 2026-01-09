using TentMan.Application.Abstractions;
using TentMan.Contracts.Enums;
using TentMan.Contracts.TenantPortal;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.TenantPortal.Queries;

public class GetMoveInHandoverQueryHandler : IRequestHandler<GetMoveInHandoverQuery, MoveInHandoverResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetMoveInHandoverQueryHandler> _logger;

    public GetMoveInHandoverQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetMoveInHandoverQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MoveInHandoverResponse?> Handle(GetMoveInHandoverQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting move-in handover for user: {UserId}", request.UserId);

        // Get tenant by linked user ID
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
                   (lp.Lease.Status == LeaseStatus.Active || lp.Lease.Status == LeaseStatus.Draft))
            .Select(lp => lp.Lease)
            .FirstOrDefault();

        if (activeLease == null)
        {
            _logger.LogInformation("No active lease found for user: {UserId}", request.UserId);
            return null;
        }

        // Get the move-in handover for this lease
        var handover = await _unitOfWork.UnitHandovers.GetByLeaseIdAsync(activeLease.Id, HandoverType.MoveIn, cancellationToken);

        if (handover == null)
        {
            _logger.LogInformation("No move-in handover found for lease: {LeaseId}", activeLease.Id);
            return null;
        }

        // Map to response
        return new MoveInHandoverResponse
        {
            HandoverId = handover.Id,
            LeaseId = handover.LeaseId,
            UnitNumber = handover.Lease.Unit?.UnitNumber ?? "",
            BuildingName = handover.Lease.Unit?.Building?.Name ?? "",
            Date = handover.Date,
            IsCompleted = handover.SignedByTenant,
            Notes = handover.Notes,
            ChecklistItems = handover.ChecklistItems
                .Where(ci => !ci.IsDeleted)
                .Select(ci => new HandoverChecklistItemDto
                {
                    Id = ci.Id,
                    Category = ci.Category,
                    ItemName = ci.ItemName,
                    Condition = ci.Condition,
                    Remarks = ci.Remarks,
                    PhotoFileId = ci.PhotoFileId,
                    PhotoFileName = ci.PhotoFile?.FileName
                }).ToList(),
            MeterReadings = handover.Lease.MeterReadings
                .Where(mr => !mr.IsDeleted)
                .Select(mr => new MeterReadingDto
                {
                    MeterId = mr.Id,
                    MeterType = mr.MeterType.ToString(),
                    Reading = mr.ReadingValue,
                    ReadingDate = mr.ReadingDate
                }).ToList()
        };
    }
}

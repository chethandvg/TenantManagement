using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Application.PropertyManagement.Services;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Commands.SetUnitOwnership;

public class SetUnitOwnershipCommandHandler : BaseCommandHandler, IRequestHandler<SetUnitOwnershipCommand, MediatR.Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOwnershipService _ownershipService;

    public SetUnitOwnershipCommandHandler(
        IUnitOfWork unitOfWork,
        IOwnershipService ownershipService,
        ICurrentUser currentUser,
        ILogger<SetUnitOwnershipCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
        _ownershipService = ownershipService;
    }

    public async Task<MediatR.Unit> Handle(SetUnitOwnershipCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Setting ownership for unit: {UnitId}", request.UnitId);

        // Validate unit exists
        var unit = await _unitOfWork.Units.GetByIdWithDetailsAsync(request.UnitId, cancellationToken);
        if (unit == null)
        {
            throw new InvalidOperationException($"Unit {request.UnitId} not found");
        }

        // Validate ownership shares
        var sharePercentages = request.Shares.Select(s => s.SharePercent).ToList();
        
        // Check that all shares are > 0
        if (sharePercentages.Any(s => s <= 0))
        {
            throw new InvalidOperationException("All ownership shares must be greater than 0");
        }

        // Check that shares sum to 100%
        if (!_ownershipService.ValidateOwnershipShares(sharePercentages))
        {
            throw new InvalidOperationException(_ownershipService.GetOwnershipValidationError(sharePercentages));
        }

        // Check that each owner appears only once
        var ownerIds = request.Shares.Select(s => s.OwnerId).ToList();
        if (ownerIds.Count != ownerIds.Distinct().Count())
        {
            throw new InvalidOperationException("Each owner can only appear once in ownership shares");
        }

        // Verify all owners exist
        foreach (var ownerId in ownerIds)
        {
            if (!await _unitOfWork.Owners.ExistsAsync(ownerId, cancellationToken))
            {
                throw new InvalidOperationException($"Owner {ownerId} not found");
            }
        }

        // Close existing ownership shares (set EffectiveTo)
        var existingShares = unit.OwnershipShares
            .Where(s => s.EffectiveTo == null)
            .ToList();

        foreach (var share in existingShares)
        {
            share.EffectiveTo = request.EffectiveFrom.AddSeconds(-1);
        }

        // Add new ownership shares
        foreach (var shareRequest in request.Shares)
        {
            var share = new UnitOwnershipShare
            {
                UnitId = request.UnitId,
                OwnerId = shareRequest.OwnerId,
                SharePercent = shareRequest.SharePercent,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = null
            };
            unit.OwnershipShares.Add(share);
        }

        // Mark that this unit has ownership override
        unit.HasUnitOwnershipOverride = true;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Ownership set for unit: {UnitId}", request.UnitId);

        return MediatR.Unit.Value;
    }
}

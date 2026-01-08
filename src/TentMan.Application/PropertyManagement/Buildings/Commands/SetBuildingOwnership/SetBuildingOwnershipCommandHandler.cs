using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Application.PropertyManagement.Services;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingOwnership;

public class SetBuildingOwnershipCommandHandler : BaseCommandHandler, IRequestHandler<SetBuildingOwnershipCommand, MediatR.Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOwnershipService _ownershipService;

    public SetBuildingOwnershipCommandHandler(
        IUnitOfWork unitOfWork,
        IOwnershipService ownershipService,
        ICurrentUser currentUser,
        ILogger<SetBuildingOwnershipCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
        _ownershipService = ownershipService;
    }

    public async Task<MediatR.Unit> Handle(SetBuildingOwnershipCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Setting ownership for building: {BuildingId}", request.BuildingId);

        // Validate building exists
        var building = await _unitOfWork.Buildings.GetByIdAsync(request.BuildingId, cancellationToken);
        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
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
        var existingShares = building.OwnershipShares
            .Where(s => s.EffectiveTo == null)
            .ToList();

        foreach (var share in existingShares)
        {
            share.EffectiveTo = request.EffectiveFrom.AddSeconds(-1);
        }

        // Add new ownership shares
        foreach (var shareRequest in request.Shares)
        {
            var share = new BuildingOwnershipShare
            {
                BuildingId = request.BuildingId,
                OwnerId = shareRequest.OwnerId,
                SharePercent = shareRequest.SharePercent,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = null
            };
            building.OwnershipShares.Add(share);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Ownership set for building: {BuildingId}", request.BuildingId);

        return MediatR.Unit.Value;
    }
}

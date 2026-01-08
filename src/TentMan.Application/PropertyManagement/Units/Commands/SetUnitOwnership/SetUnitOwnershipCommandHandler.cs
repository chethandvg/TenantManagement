using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Application.PropertyManagement.Services;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Units;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Commands.SetUnitOwnership;

public class SetUnitOwnershipCommandHandler : BaseCommandHandler, IRequestHandler<SetUnitOwnershipCommand, UnitDetailDto>
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

    public async Task<UnitDetailDto> Handle(SetUnitOwnershipCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Setting ownership shares for unit: {UnitId}", request.UnitId);

        // Validate shares using the shared service
        _ownershipService.ValidateOwnershipShareRequests(request.Shares);

        var unit = await _unitOfWork.Units.GetByIdWithDetailsAsync(request.UnitId, cancellationToken);

        if (unit == null)
        {
            throw new InvalidOperationException($"Unit {request.UnitId} not found");
        }

        // Validate all owners exist - filter to owner IDs first
        var ownerIds = request.Shares.Select(s => s.OwnerId).ToList();
        foreach (var ownerId in ownerIds)
        {
            if (!await _unitOfWork.Owners.ExistsAsync(ownerId, cancellationToken))
            {
                throw new InvalidOperationException($"Owner {ownerId} not found");
            }
        }

        // End current ownership shares
        var currentShares = unit.OwnershipShares
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow)
            .ToList();

        foreach (var share in currentShares)
        {
            share.EffectiveTo = request.EffectiveFrom.AddSeconds(-1);
            share.ModifiedAtUtc = DateTime.UtcNow;
            share.ModifiedBy = CurrentUser.UserId ?? "System";
        }

        // Add new ownership shares using Select for mapping
        var newShares = request.Shares.Select(shareRequest => new UnitOwnershipShare
        {
            UnitId = unit.Id,
            OwnerId = shareRequest.OwnerId,
            SharePercent = shareRequest.SharePercent,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = null,
            CreatedBy = CurrentUser.UserId ?? "System"
        }).ToList();

        foreach (var newShare in newShares)
        {
            unit.OwnershipShares.Add(newShare);
        }

        // Set the unit override flag
        unit.HasUnitOwnershipOverride = true;
        unit.ModifiedAtUtc = DateTime.UtcNow;
        unit.ModifiedBy = CurrentUser.UserId ?? "System";

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to get owner details
        unit = await _unitOfWork.Units.GetByIdWithDetailsAsync(request.UnitId, cancellationToken);

        Logger.LogInformation("Ownership shares set for unit: {UnitId}", unit!.Id);

        var resolvedOwnership = unit.OwnershipShares
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow)
            .Select(s => new OwnerShareDto
            {
                OwnerId = s.OwnerId,
                OwnerName = s.Owner?.DisplayName ?? "",
                SharePercent = s.SharePercent
            }).ToList();

        return new UnitDetailDto
        {
            Id = unit.Id,
            BuildingId = unit.BuildingId,
            UnitNumber = unit.UnitNumber,
            Floor = unit.Floor,
            UnitType = unit.UnitType,
            AreaSqFt = unit.AreaSqFt,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            Furnishing = unit.Furnishing,
            ParkingSlots = unit.ParkingSlots,
            OccupancyStatus = unit.OccupancyStatus,
            HasUnitOwnershipOverride = unit.HasUnitOwnershipOverride,
            ResolvedOwnership = resolvedOwnership,
            Meters = unit.Meters
                .Where(m => m.IsActive)
                .Select(m => new UnitMeterDto
                {
                    Id = m.Id,
                    UtilityType = m.UtilityType,
                    MeterNumber = m.MeterNumber,
                    Provider = m.Provider,
                    ConsumerAccount = m.ConsumerAccount,
                    IsActive = m.IsActive
                }).ToList(),
            Files = unit.UnitFiles
                .OrderBy(f => f.SortOrder)
                .Select(f => new FileDto
                {
                    FileId = f.FileId,
                    FileName = f.File?.FileName ?? "",
                    FileTag = f.FileTag,
                    SizeBytes = f.File?.SizeBytes ?? 0,
                    ContentType = f.File?.ContentType ?? "",
                    SortOrder = f.SortOrder
                }).ToList(),
            RowVersion = unit.RowVersion
        };
    }
}

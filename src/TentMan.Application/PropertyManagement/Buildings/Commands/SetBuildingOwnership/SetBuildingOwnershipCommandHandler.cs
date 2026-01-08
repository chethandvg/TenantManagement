using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Application.PropertyManagement.Services;
using TentMan.Contracts.Buildings;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingOwnership;

public class SetBuildingOwnershipCommandHandler : BaseCommandHandler, IRequestHandler<SetBuildingOwnershipCommand, BuildingDetailDto>
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

    public async Task<BuildingDetailDto> Handle(SetBuildingOwnershipCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Setting ownership shares for building: {BuildingId}", request.BuildingId);

        // Validate shares using the shared service
        _ownershipService.ValidateOwnershipShareRequests(request.Shares);

        var building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        // Validate all owners exist - filter to non-existing owners
        var ownerIds = request.Shares.Select(s => s.OwnerId).ToList();
        foreach (var ownerId in ownerIds)
        {
            if (!await _unitOfWork.Owners.ExistsAsync(ownerId, cancellationToken))
            {
                throw new InvalidOperationException($"Owner {ownerId} not found");
            }
        }

        // End current ownership shares
        var currentShares = building.OwnershipShares
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow)
            .ToList();

        foreach (var share in currentShares)
        {
            share.EffectiveTo = request.EffectiveFrom.AddSeconds(-1);
            share.ModifiedAtUtc = DateTime.UtcNow;
            share.ModifiedBy = CurrentUser.UserId ?? "System";
        }

        // Add new ownership shares using Select for mapping
        var newShares = request.Shares.Select(shareRequest => new BuildingOwnershipShare
        {
            BuildingId = building.Id,
            OwnerId = shareRequest.OwnerId,
            SharePercent = shareRequest.SharePercent,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = null,
            CreatedBy = CurrentUser.UserId ?? "System"
        }).ToList();

        foreach (var newShare in newShares)
        {
            building.OwnershipShares.Add(newShare);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to get owner details
        building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        Logger.LogInformation("Ownership shares set for building: {BuildingId}", building!.Id);

        return new BuildingDetailDto
        {
            Id = building.Id,
            OrgId = building.OrgId,
            BuildingCode = building.BuildingCode,
            Name = building.Name,
            PropertyType = building.PropertyType,
            TotalFloors = building.TotalFloors,
            HasLift = building.HasLift,
            Notes = building.Notes,
            Address = building.Address != null ? new BuildingAddressDto
            {
                Line1 = building.Address.Line1,
                Line2 = building.Address.Line2,
                Locality = building.Address.Locality,
                City = building.Address.City,
                District = building.Address.District,
                State = building.Address.State,
                PostalCode = building.Address.PostalCode,
                Landmark = building.Address.Landmark,
                Latitude = building.Address.Latitude,
                Longitude = building.Address.Longitude
            } : null,
            CurrentOwners = building.OwnershipShares
                .Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow)
                .Select(s => new OwnerShareDto
                {
                    OwnerId = s.OwnerId,
                    OwnerName = s.Owner?.DisplayName ?? "",
                    SharePercent = s.SharePercent
                }).ToList(),
            Files = building.BuildingFiles
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
            RowVersion = building.RowVersion
        };
    }
}

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

        // Validate shares
        ValidateOwnershipShares(request.Shares);

        var building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        // Validate all owners exist
        foreach (var share in request.Shares)
        {
            if (!await _unitOfWork.Owners.ExistsAsync(share.OwnerId, cancellationToken))
            {
                throw new InvalidOperationException($"Owner {share.OwnerId} not found");
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

        // Add new ownership shares
        foreach (var shareRequest in request.Shares)
        {
            var newShare = new BuildingOwnershipShare
            {
                BuildingId = building.Id,
                OwnerId = shareRequest.OwnerId,
                SharePercent = shareRequest.SharePercent,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = null,
                CreatedBy = CurrentUser.UserId ?? "System"
            };
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

    private void ValidateOwnershipShares(List<OwnershipShareRequest> shares)
    {
        if (shares == null || shares.Count == 0)
        {
            throw new InvalidOperationException("At least one ownership share is required");
        }

        // Check for duplicate owners
        var duplicateOwners = shares.GroupBy(s => s.OwnerId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateOwners.Any())
        {
            throw new InvalidOperationException("Each owner can only appear once in the ownership set");
        }

        // Check all shares are positive
        var invalidShares = shares.Where(s => s.SharePercent <= 0).ToList();
        if (invalidShares.Any())
        {
            throw new InvalidOperationException("All ownership shares must be greater than 0");
        }

        // Validate sum equals 100%
        var sharePercents = shares.Select(s => s.SharePercent);
        if (!_ownershipService.ValidateOwnershipShares(sharePercents))
        {
            var error = _ownershipService.GetOwnershipValidationError(sharePercents);
            throw new InvalidOperationException(error);
        }
    }
}

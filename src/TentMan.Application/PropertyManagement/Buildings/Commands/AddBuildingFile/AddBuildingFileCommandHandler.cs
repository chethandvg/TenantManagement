using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Buildings;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.AddBuildingFile;

public class AddBuildingFileCommandHandler : BaseCommandHandler, IRequestHandler<AddBuildingFileCommand, BuildingDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddBuildingFileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AddBuildingFileCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BuildingDetailDto> Handle(AddBuildingFileCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Adding file {FileId} to building: {BuildingId}", request.FileId, request.BuildingId);

        var building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        // Check if file is already linked
        if (building.BuildingFiles.Any(f => f.FileId == request.FileId))
        {
            throw new InvalidOperationException($"File {request.FileId} is already linked to this building");
        }

        var buildingFile = new BuildingFile
        {
            BuildingId = building.Id,
            FileId = request.FileId,
            FileTag = request.FileTag,
            SortOrder = request.SortOrder,
            CreatedBy = CurrentUser.UserId ?? "System"
        };

        building.BuildingFiles.Add(buildingFile);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to get file details
        building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        Logger.LogInformation("File {FileId} added to building: {BuildingId}", request.FileId, building!.Id);

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

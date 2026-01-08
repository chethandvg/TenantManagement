using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Buildings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Commands.UpdateBuilding;

public class UpdateBuildingCommandHandler : BaseCommandHandler, IRequestHandler<UpdateBuildingCommand, BuildingDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBuildingCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateBuildingCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BuildingDetailDto> Handle(UpdateBuildingCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Updating building: {BuildingId}", request.BuildingId);

        var building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        if (building == null)
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        building.Name = request.Name;
        building.PropertyType = request.PropertyType;
        building.TotalFloors = request.TotalFloors;
        building.HasLift = request.HasLift;
        building.Notes = request.Notes;
        building.ModifiedAtUtc = DateTime.UtcNow;
        building.ModifiedBy = CurrentUser.UserId ?? "System";

        await _unitOfWork.Buildings.UpdateAsync(building, request.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Building updated: {BuildingId}", building.Id);

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

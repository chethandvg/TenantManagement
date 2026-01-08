using TentMan.Application.Abstractions;
using TentMan.Contracts.Buildings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Queries.GetBuilding;

public class GetBuildingQueryHandler : IRequestHandler<GetBuildingQuery, BuildingDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetBuildingQueryHandler> _logger;

    public GetBuildingQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetBuildingQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BuildingDetailDto?> Handle(GetBuildingQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting building: {BuildingId}", request.BuildingId);

        var building = await _unitOfWork.Buildings.GetByIdWithDetailsAsync(request.BuildingId, cancellationToken);

        if (building == null)
        {
            return null;
        }

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

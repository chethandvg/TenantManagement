using TentMan.Application.Abstractions;
using TentMan.Contracts.Buildings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildingById;

public class GetBuildingByIdQueryHandler : IRequestHandler<GetBuildingByIdQuery, BuildingDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetBuildingByIdQueryHandler> _logger;

    public GetBuildingByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetBuildingByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BuildingDetailDto?> Handle(GetBuildingByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting building with ID: {BuildingId}", request.BuildingId);

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
            RowVersion = building.RowVersion
        };
    }
}

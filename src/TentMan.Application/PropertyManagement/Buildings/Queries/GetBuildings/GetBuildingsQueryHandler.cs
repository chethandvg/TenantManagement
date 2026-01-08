using TentMan.Application.Abstractions;
using TentMan.Contracts.Buildings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildings;

public class GetBuildingsQueryHandler : IRequestHandler<GetBuildingsQuery, IEnumerable<BuildingListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetBuildingsQueryHandler> _logger;

    public GetBuildingsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetBuildingsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<BuildingListDto>> Handle(GetBuildingsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting buildings for organization: {OrgId}", request.OrgId);

        var buildings = await _unitOfWork.Buildings.GetByOrganizationIdAsync(request.OrgId, cancellationToken);

        return buildings.Select(b => new BuildingListDto
        {
            Id = b.Id,
            BuildingCode = b.BuildingCode,
            Name = b.Name,
            PropertyType = b.PropertyType,
            TotalFloors = b.TotalFloors,
            HasLift = b.HasLift,
            UnitCount = b.Units?.Count ?? 0,
            City = b.Address?.City ?? "",
            State = b.Address?.State ?? "",
            RowVersion = b.RowVersion
        });
    }
}

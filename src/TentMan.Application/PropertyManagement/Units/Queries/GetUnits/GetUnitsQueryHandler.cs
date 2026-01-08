using TentMan.Application.Abstractions;
using TentMan.Contracts.Units;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Queries.GetUnits;

public class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, IEnumerable<UnitListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUnitsQueryHandler> _logger;

    public GetUnitsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUnitsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<UnitListDto>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting units for building: {BuildingId}", request.BuildingId);

        var units = await _unitOfWork.Units.GetByBuildingIdAsync(request.BuildingId, cancellationToken);

        return units.Select(u => new UnitListDto
        {
            Id = u.Id,
            UnitNumber = u.UnitNumber,
            Floor = u.Floor,
            UnitType = u.UnitType,
            AreaSqFt = u.AreaSqFt,
            Bedrooms = u.Bedrooms,
            Bathrooms = u.Bathrooms,
            Furnishing = u.Furnishing,
            OccupancyStatus = u.OccupancyStatus,
            RowVersion = u.RowVersion
        });
    }
}

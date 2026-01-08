using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Units;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Commands.BulkCreateUnits;

public class BulkCreateUnitsCommandHandler : BaseCommandHandler, IRequestHandler<BulkCreateUnitsCommand, List<UnitListDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public BulkCreateUnitsCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<BulkCreateUnitsCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UnitListDto>> Handle(BulkCreateUnitsCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Bulk creating {Count} units for building: {BuildingId}", request.Units.Count, request.BuildingId);

        // Validate building exists
        if (!await _unitOfWork.Buildings.ExistsAsync(request.BuildingId, cancellationToken))
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        // Validate all unit numbers are unique within the request
        var unitNumbers = request.Units.Select(u => u.UnitNumber).ToList();
        if (unitNumbers.Count != unitNumbers.Distinct().Count())
        {
            throw new InvalidOperationException("Unit numbers must be unique within the request");
        }

        // Validate unit numbers don't already exist in the building
        foreach (var unitRequest in request.Units)
        {
            if (await _unitOfWork.Units.UnitNumberExistsAsync(request.BuildingId, unitRequest.UnitNumber, null, cancellationToken))
            {
                throw new InvalidOperationException($"Unit number '{unitRequest.UnitNumber}' already exists in this building");
            }
        }

        var units = new List<TentMan.Domain.Entities.Unit>();
        foreach (var unitRequest in request.Units)
        {
            var unit = new TentMan.Domain.Entities.Unit
            {
                BuildingId = request.BuildingId,
                UnitNumber = unitRequest.UnitNumber,
                Floor = unitRequest.Floor,
                UnitType = unitRequest.UnitType,
                AreaSqFt = unitRequest.AreaSqFt,
                Bedrooms = unitRequest.Bedrooms,
                Bathrooms = unitRequest.Bathrooms,
                Furnishing = unitRequest.Furnishing,
                ParkingSlots = unitRequest.ParkingSlots
            };
            units.Add(unit);
        }

        await _unitOfWork.Units.AddRangeAsync(units, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Bulk created {Count} units for building: {BuildingId}", units.Count, request.BuildingId);

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
        }).ToList();
    }
}

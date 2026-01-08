using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Units;
using TentMan.Contracts.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using DomainUnit = TentMan.Domain.Entities.Unit;

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
        Logger.LogInformation("Bulk creating {Count} units in building: {BuildingId}", request.Units.Count, request.BuildingId);

        // Validate building exists
        if (!await _unitOfWork.Buildings.ExistsAsync(request.BuildingId, cancellationToken))
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        // Check for duplicate unit numbers within request
        var duplicateUnitNumbers = request.Units
            .GroupBy(u => u.UnitNumber)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateUnitNumbers.Any())
        {
            throw new InvalidOperationException($"Duplicate unit numbers in request: {string.Join(", ", duplicateUnitNumbers)}");
        }

        // Check for existing unit numbers in database
        foreach (var unitData in request.Units)
        {
            if (await _unitOfWork.Units.UnitNumberExistsAsync(request.BuildingId, unitData.UnitNumber, null, cancellationToken))
            {
                throw new InvalidOperationException($"Unit number '{unitData.UnitNumber}' already exists in this building");
            }
        }

        var units = request.Units.Select(u => new DomainUnit
        {
            BuildingId = request.BuildingId,
            UnitNumber = u.UnitNumber,
            Floor = u.Floor,
            UnitType = u.UnitType,
            AreaSqFt = u.AreaSqFt,
            Bedrooms = u.Bedrooms,
            Bathrooms = u.Bathrooms,
            Furnishing = u.Furnishing,
            ParkingSlots = u.ParkingSlots,
            OccupancyStatus = OccupancyStatus.Vacant,
            CreatedBy = CurrentUser.UserId ?? "System"
        }).ToList();

        await _unitOfWork.Units.AddRangeAsync(units, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Bulk created {Count} units in building: {BuildingId}", units.Count, request.BuildingId);

        return units.Select(unit => new UnitListDto
        {
            Id = unit.Id,
            UnitNumber = unit.UnitNumber,
            Floor = unit.Floor,
            UnitType = unit.UnitType,
            AreaSqFt = unit.AreaSqFt,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            Furnishing = unit.Furnishing,
            OccupancyStatus = unit.OccupancyStatus,
            RowVersion = unit.RowVersion
        }).ToList();
    }
}

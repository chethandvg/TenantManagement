using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Units;
using TentMan.Contracts.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using DomainUnit = TentMan.Domain.Entities.Unit;

namespace TentMan.Application.PropertyManagement.Units.Commands.CreateUnit;

public class CreateUnitCommandHandler : BaseCommandHandler, IRequestHandler<CreateUnitCommand, UnitListDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUnitCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateUnitCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitListDto> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating unit: {UnitNumber} in building: {BuildingId}", request.UnitNumber, request.BuildingId);

        // Validate building exists
        if (!await _unitOfWork.Buildings.ExistsAsync(request.BuildingId, cancellationToken))
        {
            throw new InvalidOperationException($"Building {request.BuildingId} not found");
        }

        // Check for duplicate unit number
        if (await _unitOfWork.Units.UnitNumberExistsAsync(request.BuildingId, request.UnitNumber, null, cancellationToken))
        {
            throw new InvalidOperationException($"Unit number '{request.UnitNumber}' already exists in this building");
        }

        var unit = new DomainUnit
        {
            BuildingId = request.BuildingId,
            UnitNumber = request.UnitNumber,
            Floor = request.Floor,
            UnitType = request.UnitType,
            AreaSqFt = request.AreaSqFt,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            Furnishing = request.Furnishing,
            ParkingSlots = request.ParkingSlots,
            OccupancyStatus = OccupancyStatus.Vacant
        };

        await _unitOfWork.Units.AddAsync(unit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Unit created with ID: {UnitId}", unit.Id);

        return new UnitListDto
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
        };
    }
}

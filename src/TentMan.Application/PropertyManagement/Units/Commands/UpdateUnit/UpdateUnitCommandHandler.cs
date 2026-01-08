using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Units;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Units.Commands.UpdateUnit;

public class UpdateUnitCommandHandler : BaseCommandHandler, IRequestHandler<UpdateUnitCommand, UnitDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUnitCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateUnitCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UnitDetailDto> Handle(UpdateUnitCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Updating unit: {UnitId}", request.Id);

        var unit = await _unitOfWork.Units.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (unit == null)
        {
            throw new InvalidOperationException($"Unit {request.Id} not found");
        }

        // Update properties (UnitNumber is immutable, so not updating it)
        unit.Floor = request.Floor;
        unit.UnitType = request.UnitType;
        unit.AreaSqFt = request.AreaSqFt;
        unit.Bedrooms = request.Bedrooms;
        unit.Bathrooms = request.Bathrooms;
        unit.Furnishing = request.Furnishing;
        unit.ParkingSlots = request.ParkingSlots;
        unit.OccupancyStatus = request.OccupancyStatus;

        await _unitOfWork.Units.UpdateAsync(unit, request.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Unit updated: {UnitId}", unit.Id);

        return new UnitDetailDto
        {
            Id = unit.Id,
            BuildingId = unit.BuildingId,
            UnitNumber = unit.UnitNumber,
            Floor = unit.Floor,
            UnitType = unit.UnitType,
            AreaSqFt = unit.AreaSqFt,
            Bedrooms = unit.Bedrooms,
            Bathrooms = unit.Bathrooms,
            Furnishing = unit.Furnishing,
            ParkingSlots = unit.ParkingSlots,
            OccupancyStatus = unit.OccupancyStatus,
            HasUnitOwnershipOverride = unit.HasUnitOwnershipOverride,
            RowVersion = unit.RowVersion
        };
    }
}

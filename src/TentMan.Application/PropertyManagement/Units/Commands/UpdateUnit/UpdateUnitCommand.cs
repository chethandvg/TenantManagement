using TentMan.Contracts.Units;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.UpdateUnit;

public record UpdateUnitCommand(
    Guid UnitId,
    int Floor,
    UnitType UnitType,
    decimal AreaSqFt,
    int Bedrooms,
    int Bathrooms,
    Furnishing Furnishing,
    int ParkingSlots,
    OccupancyStatus OccupancyStatus,
    byte[] RowVersion
) : IRequest<UnitDetailDto>;

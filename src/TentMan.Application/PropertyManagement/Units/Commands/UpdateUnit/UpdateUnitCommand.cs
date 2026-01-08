using TentMan.Contracts.Enums;
using TentMan.Contracts.Units;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.UpdateUnit;

public record UpdateUnitCommand(
    Guid Id,
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

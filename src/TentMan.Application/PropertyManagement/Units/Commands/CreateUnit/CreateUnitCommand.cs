using TentMan.Contracts.Units;
using TentMan.Domain.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.CreateUnit;

public record CreateUnitCommand(
    Guid BuildingId,
    string UnitNumber,
    int Floor,
    UnitType UnitType,
    decimal AreaSqFt,
    int Bedrooms,
    int Bathrooms,
    Furnishing Furnishing,
    int ParkingSlots
) : IRequest<UnitListDto>;

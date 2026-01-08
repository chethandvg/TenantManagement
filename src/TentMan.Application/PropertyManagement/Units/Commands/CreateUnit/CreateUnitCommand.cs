using TentMan.Contracts.Units;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.CreateUnit;

public record CreateUnitCommand(
    Guid BuildingId,
    string UnitNumber,
    int Floor,
    TentMan.Domain.Enums.UnitType UnitType,
    decimal AreaSqFt,
    int Bedrooms,
    int Bathrooms,
    TentMan.Domain.Enums.Furnishing Furnishing,
    int ParkingSlots
) : IRequest<UnitListDto>;

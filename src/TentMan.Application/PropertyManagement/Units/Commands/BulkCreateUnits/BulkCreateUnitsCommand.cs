using TentMan.Contracts.Units;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.PropertyManagement.Units.Commands.BulkCreateUnits;

public record BulkCreateUnitsCommand(
    Guid BuildingId,
    List<CreateUnitData> Units
) : IRequest<List<UnitListDto>>;

public record CreateUnitData(
    string UnitNumber,
    int Floor,
    UnitType UnitType,
    decimal AreaSqFt,
    int Bedrooms,
    int Bathrooms,
    Furnishing Furnishing,
    int ParkingSlots
);

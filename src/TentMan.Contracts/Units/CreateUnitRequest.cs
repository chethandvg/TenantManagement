using TentMan.Domain.Enums;

namespace TentMan.Contracts.Units;

public sealed class CreateUnitRequest
{
    public Guid BuildingId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public int Floor { get; init; }
    public UnitType UnitType { get; init; }
    public decimal AreaSqFt { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public Furnishing Furnishing { get; init; }
    public int ParkingSlots { get; init; }
}

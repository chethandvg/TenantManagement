using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Units;

public sealed class UnitListDto
{
    public Guid Id { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public int Floor { get; init; }
    public UnitType UnitType { get; init; }
    public decimal AreaSqFt { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public Furnishing Furnishing { get; init; }
    public OccupancyStatus OccupancyStatus { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

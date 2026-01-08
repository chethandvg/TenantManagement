using TentMan.Domain.Enums;
using TentMan.Contracts.Buildings;

namespace TentMan.Contracts.Units;

public sealed class UnitDetailDto
{
    public Guid Id { get; init; }
    public Guid BuildingId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public int Floor { get; init; }
    public UnitType UnitType { get; init; }
    public decimal AreaSqFt { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public Furnishing Furnishing { get; init; }
    public int ParkingSlots { get; init; }
    public OccupancyStatus OccupancyStatus { get; init; }
    public bool HasUnitOwnershipOverride { get; init; }
    public List<OwnerShareDto> ResolvedOwnership { get; init; } = new();
    public List<UnitMeterDto> Meters { get; init; } = new();
    public List<FileDto> Files { get; init; } = new();
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public sealed class UnitMeterDto
{
    public Guid Id { get; init; }
    public UtilityType UtilityType { get; init; }
    public string MeterNumber { get; init; } = string.Empty;
    public string? Provider { get; init; }
    public string? ConsumerAccount { get; init; }
    public bool IsActive { get; init; }
}

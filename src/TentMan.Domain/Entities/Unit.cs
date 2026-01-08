using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a unit (flat/house/shop/office) within a building.
/// </summary>
public class Unit : BaseEntity
{
    public Unit()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        OccupancyStatus = OccupancyStatus.Vacant;
        HasUnitOwnershipOverride = false;
    }

    public Guid BuildingId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public UnitType UnitType { get; set; }
    public decimal AreaSqFt { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public Furnishing Furnishing { get; set; }
    public int ParkingSlots { get; set; }
    public OccupancyStatus OccupancyStatus { get; set; }
    public bool HasUnitOwnershipOverride { get; set; }

    // Navigation properties
    public Building Building { get; set; } = null!;
    public ICollection<UnitOwnershipShare> OwnershipShares { get; set; } = new List<UnitOwnershipShare>();
    public ICollection<UnitMeter> Meters { get; set; } = new List<UnitMeter>();
    public ICollection<UnitFile> UnitFiles { get; set; } = new List<UnitFile>();
}

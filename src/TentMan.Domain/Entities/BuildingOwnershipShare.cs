using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents ownership share of a building.
/// Default ownership applies to all units unless overridden.
/// </summary>
public class BuildingOwnershipShare : BaseEntity
{
    public BuildingOwnershipShare()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid BuildingId { get; set; }
    public Guid OwnerId { get; set; }
    public decimal SharePercent { get; set; } // 0-100, precision (5,2)
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; } // null for current ownership

    // Navigation properties
    public Building Building { get; set; } = null!;
    public Owner Owner { get; set; } = null!;
}

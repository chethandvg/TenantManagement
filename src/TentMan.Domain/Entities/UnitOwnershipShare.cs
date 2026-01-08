using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents ownership share of a specific unit (overrides building ownership).
/// </summary>
public class UnitOwnershipShare : BaseEntity
{
    public UnitOwnershipShare()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid UnitId { get; set; }
    public Guid OwnerId { get; set; }
    public decimal SharePercent { get; set; } // 0-100, precision (5,2)
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; } // null for current ownership

    // Navigation properties
    public Unit Unit { get; set; } = null!;
    public Owner Owner { get; set; } = null!;
}

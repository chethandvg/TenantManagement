using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a slab (tier) in a utility rate plan.
/// E.g., 0-100 units @ $0.10/unit, 101-200 @ $0.15/unit, etc.
/// </summary>
public class UtilityRateSlab : BaseEntity
{
    public UtilityRateSlab()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid UtilityRatePlanId { get; set; }
    public int SlabOrder { get; set; } // 1, 2, 3, etc.
    public decimal FromUnits { get; set; }
    public decimal? ToUnits { get; set; } // Null = unlimited
    public decimal RatePerUnit { get; set; }
    public decimal? FixedCharge { get; set; } // Optional fixed charge for this slab

    // Navigation properties
    public UtilityRatePlan UtilityRatePlan { get; set; } = null!;
}

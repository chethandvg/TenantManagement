using TentMan.Domain.Common;
using TentMan.Domain.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a utility meter associated with a unit.
/// </summary>
public class UnitMeter : BaseEntity
{
    public UnitMeter()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsActive = true;
    }

    public Guid UnitId { get; set; }
    public UtilityType UtilityType { get; set; }
    public string MeterNumber { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ConsumerAccount { get; set; }
    public bool IsActive { get; set; }

    // Navigation property
    public Unit Unit { get; set; } = null!;
}

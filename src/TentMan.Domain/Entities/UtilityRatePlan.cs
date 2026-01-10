using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a utility rate plan for calculating charges.
/// E.g., electricity tariff structure with different slabs.
/// </summary>
public class UtilityRatePlan : BaseEntity
{
    public UtilityRatePlan()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsActive = true;
    }

    public Guid OrgId { get; set; }
    public UtilityType UtilityType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<UtilityRateSlab> RateSlabs { get; set; } = new List<UtilityRateSlab>();
    public ICollection<UtilityStatement> UtilityStatements { get; set; } = new List<UtilityStatement>();
}

using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a building/property within an organization.
/// </summary>
public class Building : BaseEntity
{
    public Building()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid OrgId { get; set; }
    public string BuildingCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public int TotalFloors { get; set; }
    public bool HasLift { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public BuildingAddress? Address { get; set; }
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<BuildingOwnershipShare> OwnershipShares { get; set; } = new List<BuildingOwnershipShare>();
    public ICollection<BuildingFile> BuildingFiles { get; set; } = new List<BuildingFile>();
}

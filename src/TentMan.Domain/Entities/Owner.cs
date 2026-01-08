using TentMan.Domain.Common;
using TentMan.Domain.Enums;
using TentMan.Domain.Entities.Identity;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an owner (individual or company) of properties.
/// </summary>
public class Owner : BaseEntity
{
    public Owner()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid OrgId { get; set; }
    public OwnerType OwnerType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Pan { get; set; } // Indian PAN
    public string? Gstin { get; set; } // Indian GSTIN
    public Guid? LinkedUserId { get; set; } // Link to user account for owner portal

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ApplicationUser? LinkedUser { get; set; }
    public ICollection<BuildingOwnershipShare> BuildingOwnershipShares { get; set; } = new List<BuildingOwnershipShare>();
    public ICollection<UnitOwnershipShare> UnitOwnershipShares { get; set; } = new List<UnitOwnershipShare>();
}

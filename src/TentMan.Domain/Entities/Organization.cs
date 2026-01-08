using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an organization/account for multi-tenancy support.
/// </summary>
public class Organization : BaseEntity
{
    public Organization()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        TimeZone = "Asia/Kolkata"; // Default India timezone
        IsActive = true;
    }

    public string Name { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Building> Buildings { get; set; } = new List<Building>();
    public ICollection<Owner> Owners { get; set; } = new List<Owner>();
    public ICollection<FileMetadata> Files { get; set; } = new List<FileMetadata>();
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}

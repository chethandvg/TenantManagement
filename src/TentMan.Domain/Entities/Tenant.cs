using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a tenant (person who can log in, pay rent, and be responsible for a lease).
/// </summary>
public class Tenant : BaseEntity
{
    public Tenant()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsActive = true;
    }

    public Guid OrgId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty; // E.164 format
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<TenantAddress> Addresses { get; set; } = new List<TenantAddress>();
    public ICollection<TenantEmergencyContact> EmergencyContacts { get; set; } = new List<TenantEmergencyContact>();
    public ICollection<TenantDocument> Documents { get; set; } = new List<TenantDocument>();
    public ICollection<LeaseParty> LeaseParties { get; set; } = new List<LeaseParty>();
}

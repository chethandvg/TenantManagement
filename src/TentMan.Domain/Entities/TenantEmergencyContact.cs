using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an emergency contact for a tenant.
/// </summary>
public class TenantEmergencyContact : BaseEntity
{
    public TenantEmergencyContact()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    // Navigation property
    public Tenant Tenant { get; set; } = null!;
}

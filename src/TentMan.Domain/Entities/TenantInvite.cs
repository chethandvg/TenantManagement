using TentMan.Domain.Common;
using TentMan.Domain.Entities.Identity;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an invitation for a tenant to join the system.
/// Owner generates invite tied to tenant phone/email, tenant signs up and gets mapped.
/// </summary>
public class TenantInvite : BaseEntity
{
    public TenantInvite()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsUsed = false;
    }

    public Guid OrgId { get; set; }
    public Guid TenantId { get; set; }
    public string InviteToken { get; set; } = string.Empty; // Unique token for invite URL
    public string Phone { get; set; } = string.Empty; // E.164 format
    public string? Email { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public Guid? AcceptedByUserId { get; set; } // User who accepted the invite
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ApplicationUser? AcceptedByUser { get; set; }
}

using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a party (tenant, occupant, guarantor) in a lease agreement.
/// Join table between Lease and Tenant.
/// </summary>
public class LeaseParty : BaseEntity
{
    public LeaseParty()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        IsResponsibleForPayment = false;
    }

    public Guid LeaseId { get; set; }
    public Guid TenantId { get; set; }
    public LeasePartyRole Role { get; set; }
    public bool IsResponsibleForPayment { get; set; }
    public DateOnly? MoveInDate { get; set; } // For roommates joining later
    public DateOnly? MoveOutDate { get; set; }

    // Navigation properties
    public Lease Lease { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}

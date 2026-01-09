using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Leases;

/// <summary>
/// Request to add a party (tenant/occupant) to a lease.
/// </summary>
public class AddLeasePartyRequest
{
    public Guid TenantId { get; set; }
    public LeasePartyRole Role { get; set; }
    public bool IsResponsibleForPayment { get; set; } = false;
    public DateOnly? MoveInDate { get; set; }
}

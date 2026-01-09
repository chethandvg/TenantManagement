namespace TentMan.Contracts.Leases;

/// <summary>
/// Request to activate a lease.
/// </summary>
public class ActivateLeaseRequest
{
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

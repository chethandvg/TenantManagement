namespace TentMan.Contracts.Leases;

/// <summary>
/// Request to end a lease.
/// </summary>
public class EndLeaseRequest
{
    public DateOnly EndDate { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

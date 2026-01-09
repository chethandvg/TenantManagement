namespace TentMan.Contracts.TenantInvites;

/// <summary>
/// Request to generate a tenant invite.
/// </summary>
public sealed class GenerateInviteRequest
{
    public Guid TenantId { get; set; }
    public int ExpiryDays { get; set; } = 7; // Default 7 days
}

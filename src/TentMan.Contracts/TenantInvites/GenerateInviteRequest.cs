namespace TentMan.Contracts.TenantInvites;

/// <summary>
/// Request to generate a tenant invite.
/// </summary>
public sealed class GenerateInviteRequest
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Number of days from creation until the invite expires.
    /// Defaults to 7 days if not specified. Must be between 1 and 90 days.
    /// </summary>
    public int ExpiryDays { get; set; } = 7;
}

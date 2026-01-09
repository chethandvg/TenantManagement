namespace TentMan.Contracts.TenantInvites;

/// <summary>
/// Request to accept a tenant invite and create a user account.
/// </summary>
public sealed class AcceptInviteRequest
{
    public string InviteToken { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

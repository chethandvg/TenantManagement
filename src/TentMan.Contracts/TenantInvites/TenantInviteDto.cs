namespace TentMan.Contracts.TenantInvites;

/// <summary>
/// DTO for tenant invite information.
/// </summary>
public sealed class TenantInviteDto
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public Guid TenantId { get; set; }
    public string InviteToken { get; set; } = string.Empty;
    public string InviteUrl { get; set; } = string.Empty; // Full URL for convenience
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public string TenantFullName { get; set; } = string.Empty;
}

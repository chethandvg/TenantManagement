namespace TentMan.Contracts.TenantInvites;

/// <summary>
/// Response for validating an invite token.
/// </summary>
public sealed class ValidateInviteResponse
{
    public bool IsValid { get; set; }
    public string? TenantFullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ErrorMessage { get; set; }
}

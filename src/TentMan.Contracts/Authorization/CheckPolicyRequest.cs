namespace TentMan.Contracts.Authorization;

/// <summary>
/// Request to check if the current user satisfies a specific policy.
/// </summary>
public sealed class CheckPolicyRequest
{
    /// <summary>
    /// Gets or sets the policy name to check (e.g., "CanViewTenantPortal").
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;
}

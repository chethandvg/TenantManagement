namespace TentMan.Contracts.Authorization;

/// <summary>
/// Request to check if the current user has a specific permission.
/// </summary>
public sealed class CheckPermissionRequest
{
    /// <summary>
    /// Gets or sets the permission to check (e.g., "buildings:read").
    /// </summary>
    public string Permission { get; set; } = string.Empty;
}

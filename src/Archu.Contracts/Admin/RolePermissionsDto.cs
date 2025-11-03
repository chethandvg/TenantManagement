using System.Collections.Generic;

namespace Archu.Contracts.Admin;

/// <summary>
/// Represents the permissions assigned to a specific role.
/// </summary>
public sealed class RolePermissionsDto
{
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string NormalizedRoleName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyCollection<PermissionDto> Permissions { get; init; } = Array.Empty<PermissionDto>();
}

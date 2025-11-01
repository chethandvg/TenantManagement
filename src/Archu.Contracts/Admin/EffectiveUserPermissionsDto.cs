using System.Collections.Generic;

namespace Archu.Contracts.Admin;

/// <summary>
/// Represents the aggregate of direct and role-derived permissions for a user.
/// </summary>
public sealed class EffectiveUserPermissionsDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyCollection<PermissionDto> DirectPermissions { get; init; } = Array.Empty<PermissionDto>();
    public IReadOnlyCollection<RolePermissionsDto> RolePermissions { get; init; } = Array.Empty<RolePermissionsDto>();
    public IReadOnlyCollection<PermissionDto> EffectivePermissions { get; init; } = Array.Empty<PermissionDto>();
}

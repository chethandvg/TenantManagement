using System.Collections.Generic;

namespace Archu.Contracts.Admin;

/// <summary>
/// Represents the permissions granted directly to a user.
/// </summary>
public sealed class UserPermissionsDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyCollection<PermissionDto> Permissions { get; init; } = Array.Empty<PermissionDto>();
}

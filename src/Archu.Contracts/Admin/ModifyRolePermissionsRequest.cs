using System;
using System.Collections.Generic;

namespace Archu.Contracts.Admin;

/// <summary>
/// Request payload for modifying the permissions linked to a role.
/// Provides the collection of permission names to assign or remove.
/// </summary>
public sealed class ModifyRolePermissionsRequest
{
    /// <summary>
    /// Gets or sets the human-readable permission names to process.
    /// Values are normalized to uppercase on the server.
    /// </summary>
    public IReadOnlyCollection<string> PermissionNames { get; init; } = Array.Empty<string>();
}

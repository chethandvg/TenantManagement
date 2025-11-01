using System;
using System.Collections.Generic;

namespace Archu.Contracts.Admin;

/// <summary>
/// Request payload describing the permissions to assign or remove directly from a user.
/// </summary>
public sealed class ModifyUserPermissionsRequest
{
    /// <summary>
    /// Gets or sets the collection of permission names to process.
    /// Permission names are case-insensitive and normalized server-side.
    /// </summary>
    public IReadOnlyCollection<string> PermissionNames { get; init; } = Array.Empty<string>();
}

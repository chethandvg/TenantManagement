namespace Archu.Domain.Constants;

/// <summary>
/// Maps system roles to their assigned permission claims.
/// These permission strings are added to JWT tokens and checked by authorization handlers.
/// </summary>
public static class RolePermissionClaims
{
    private static readonly Dictionary<string, string[]> _rolePermissionClaims = new()
    {
        [RoleNames.Guest] = new[]
        {
            PermissionNames.Products.Read
        },

        [RoleNames.User] = new[]
        {
            PermissionNames.Products.Read,
            PermissionNames.Products.Create,
            PermissionNames.Products.Update
        },

        [RoleNames.Manager] = new[]
        {
            PermissionNames.Products.Read,
            PermissionNames.Products.Create,
            PermissionNames.Products.Update,
            PermissionNames.Products.Delete,
            PermissionNames.Users.Read
        },

        [RoleNames.Administrator] = new[]
        {
            PermissionNames.Products.Manage,
            PermissionNames.Users.Manage,
            PermissionNames.Roles.Read
        },

        [RoleNames.SuperAdmin] = new[]
        {
            PermissionNames.Products.Manage,
            PermissionNames.Users.Manage,
            PermissionNames.Roles.Manage
        }
    };

    /// <summary>
    /// Gets the permission claim strings for a given role.
    /// These are the permission claims that should be added to the user's JWT token.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>Array of permission claim strings for the role.</returns>
    public static string[] GetPermissionClaimsForRole(string roleName)
    {
        return _rolePermissionClaims.TryGetValue(roleName, out var permissions)
            ? permissions
            : Array.Empty<string>();
    }

    /// <summary>
    /// Gets all permission claims for multiple roles.
    /// Automatically removes duplicates.
    /// </summary>
    /// <param name="roleNames">Collection of role names.</param>
    /// <returns>Distinct array of permission claim strings.</returns>
    public static string[] GetPermissionClaimsForRoles(IEnumerable<string> roleNames)
    {
        return roleNames
            .SelectMany(GetPermissionClaimsForRole)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Gets all unique permission claims defined in the system.
    /// </summary>
    /// <returns>Array of all permission claim strings.</returns>
    public static string[] GetAllPermissionClaims()
    {
        return _rolePermissionClaims.Values
            .SelectMany(p => p)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Checks if a role has a specific permission claim.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="permissionClaim">The permission claim string to check.</param>
    /// <returns>True if the role has the permission claim.</returns>
    public static bool RoleHasPermissionClaim(string roleName, string permissionClaim)
    {
        var permissions = GetPermissionClaimsForRole(roleName);
        return permissions.Contains(permissionClaim, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets all roles that have a specific permission claim.
    /// </summary>
    /// <param name="permissionClaim">The permission claim string.</param>
    /// <returns>Array of role names that have this permission.</returns>
    public static string[] GetRolesWithPermissionClaim(string permissionClaim)
    {
        return _rolePermissionClaims
            .Where(kvp => kvp.Value.Contains(permissionClaim, StringComparer.Ordinal))
            .Select(kvp => kvp.Key)
            .ToArray();
    }
}

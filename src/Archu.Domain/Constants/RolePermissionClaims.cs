using Archu.Contracts.Authentication.Constants;

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
            PermissionNames.Roles.Manage
        },

        [RoleNames.SuperAdmin] = new[]
        {
            PermissionNames.Products.Manage,
            PermissionNames.Users.Manage,
            PermissionNames.Roles.Manage
        }
    };

    private static readonly Dictionary<string, string[]> _permissionImplications = new(StringComparer.Ordinal)
    {
        [PermissionNames.Products.Manage] = new[]
        {
            PermissionNames.Products.Read,
            PermissionNames.Products.Create,
            PermissionNames.Products.Update,
            PermissionNames.Products.Delete
        },
        [PermissionNames.Users.Manage] = new[]
        {
            PermissionNames.Users.Read,
            PermissionNames.Users.Create,
            PermissionNames.Users.Update,
            PermissionNames.Users.Delete
        },
        [PermissionNames.Roles.Manage] = new[]
        {
            PermissionNames.Roles.Read,
            PermissionNames.Roles.Create,
            PermissionNames.Roles.Update,
            PermissionNames.Roles.Delete
        }
    };

    /// <summary>
    /// Gets the permission claim strings for a given role.
    /// These are the permission claims that should be added to the user's JWT token.
    /// High-level permissions such as "manage" are expanded to include their
    /// implied granular actions.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>Array of permission claim strings for the role.</returns>
    public static string[] GetPermissionClaimsForRole(string roleName)
    {
        return _rolePermissionClaims.TryGetValue(roleName, out var permissions)
            ? ExpandPermissions(permissions)
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
            .SelectMany(ExpandPermissions)
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
            .Where(kvp => ExpandPermissions(kvp.Value).Contains(permissionClaim, StringComparer.Ordinal))
            .Select(kvp => kvp.Key)
            .ToArray();
    }

    /// <summary>
    /// Ensures that high-level permissions (e.g., manage) automatically confer
    /// their granular CRUD counterparts so authorization policies such as
    /// "products:read" succeed for administrators without duplicating claims.
    /// </summary>
    /// <param name="permissions">The base permission claims configured for a role.</param>
    /// <returns>The expanded permission claim set.</returns>
    private static string[] ExpandPermissions(IEnumerable<string> permissions)
    {
        var expanded = new HashSet<string>(permissions, StringComparer.Ordinal);

        foreach (var permission in permissions)
        {
            if (_permissionImplications.TryGetValue(permission, out var impliedPermissions))
            {
                foreach (var implied in impliedPermissions)
                {
                    expanded.Add(implied);
                }
            }
        }

        return expanded.ToArray();
    }
}

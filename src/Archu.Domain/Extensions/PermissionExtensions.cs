using Archu.Domain.Enums;

namespace Archu.Domain.Extensions;

/// <summary>
/// Extension methods for working with Permission enum.
/// Provides utility methods for permission checking and manipulation.
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// Checks if the permission set includes a specific permission.
    /// </summary>
    /// <param name="permissions">The permission set to check.</param>
    /// <param name="permission">The permission to look for.</param>
    /// <returns>True if the permission is included, otherwise false.</returns>
    public static bool HasPermission(this Permission permissions, Permission permission)
    {
        return (permissions & permission) == permission;
    }

    /// <summary>
    /// Checks if the permission set includes any of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permission set to check.</param>
    /// <param name="requiredPermissions">The permissions to look for.</param>
    /// <returns>True if any of the required permissions are included.</returns>
    public static bool HasAnyPermission(this Permission permissions, params Permission[] requiredPermissions)
    {
        return requiredPermissions.Any(p => permissions.HasPermission(p));
    }

    /// <summary>
    /// Checks if the permission set includes all of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permission set to check.</param>
    /// <param name="requiredPermissions">The permissions to look for.</param>
    /// <returns>True if all required permissions are included.</returns>
    public static bool HasAllPermissions(this Permission permissions, params Permission[] requiredPermissions)
    {
        return requiredPermissions.All(p => permissions.HasPermission(p));
    }

    /// <summary>
    /// Adds a permission to the permission set.
    /// </summary>
    /// <param name="permissions">The current permission set.</param>
    /// <param name="permission">The permission to add.</param>
    /// <returns>The updated permission set.</returns>
    public static Permission AddPermission(this Permission permissions, Permission permission)
    {
        return permissions | permission;
    }

    /// <summary>
    /// Removes a permission from the permission set.
    /// </summary>
    /// <param name="permissions">The current permission set.</param>
    /// <param name="permission">The permission to remove.</param>
    /// <returns>The updated permission set.</returns>
    public static Permission RemovePermission(this Permission permissions, Permission permission)
    {
        return permissions & ~permission;
    }

    /// <summary>
    /// Gets all individual permissions from a permission set.
    /// </summary>
    /// <param name="permissions">The permission set.</param>
    /// <returns>Collection of individual permissions.</returns>
    public static IEnumerable<Permission> GetIndividualPermissions(this Permission permissions)
    {
        return Enum.GetValues<Permission>()
            .Where(p => p != Permission.None && p != Permission.All)
            .Where(p => permissions.HasPermission(p));
    }

    /// <summary>
    /// Converts a permission set to a human-readable string list.
    /// </summary>
    /// <param name="permissions">The permission set.</param>
    /// <returns>Comma-separated list of permission names.</returns>
    public static string ToReadableString(this Permission permissions)
    {
        if (permissions == Permission.None)
            return "None";

        if (permissions == Permission.All)
            return "All";

        var individualPermissions = permissions.GetIndividualPermissions();
        return string.Join(", ", individualPermissions.Select(p => p.ToString()));
    }
}

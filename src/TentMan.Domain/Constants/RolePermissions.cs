using TentMan.Domain.Enums;

namespace TentMan.Domain.Constants;

/// <summary>
/// Maps system roles to their default permissions.
/// Use this to configure initial role permissions during system setup.
/// </summary>
public static class RolePermissions
{
    /// <summary>
    /// Gets the default permissions for the Guest role.
    /// </summary>
    public static Permission GuestPermissions => Permission.Read;

    /// <summary>
    /// Gets the default permissions for the User role.
    /// </summary>
    public static Permission UserPermissions =>
        Permission.Read |
        Permission.Create |
        Permission.Update;

    /// <summary>
    /// Gets the default permissions for the Manager role.
    /// </summary>
    public static Permission ManagerPermissions =>
        Permission.Read |
        Permission.Create |
        Permission.Update |
        Permission.Delete |
        Permission.ExportData |
        Permission.ViewAuditLogs;

    /// <summary>
    /// Gets the default permissions for the Administrator role.
    /// </summary>  
    public static Permission AdministratorPermissions =>
        Permission.Read |
        Permission.Create |
        Permission.Update |
        Permission.Delete |
        Permission.ManageUsers |
        Permission.ExportData |
        Permission.ImportData |
        Permission.ViewAuditLogs;

    /// <summary>
    /// Gets the default permissions for the SuperAdmin role.
    /// </summary>
    public static Permission SuperAdminPermissions => Permission.All;

    /// <summary>
    /// Gets the default permissions for a given role name.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <returns>Default permissions for the role, or None if role not found.</returns>
    public static Permission GetDefaultPermissions(string roleName)
    {
        return roleName switch
        {
            RoleNames.Guest => GuestPermissions,
            RoleNames.User => UserPermissions,
            RoleNames.Manager => ManagerPermissions,
            RoleNames.Administrator => AdministratorPermissions,
            RoleNames.SuperAdmin => SuperAdminPermissions,
            _ => Permission.None
        };
    }

    /// <summary>
    /// Checks if a role has a specific permission by default.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the role has the permission by default.</returns>
    public static bool RoleHasPermission(string roleName, Permission permission)
    {
        var rolePermissions = GetDefaultPermissions(roleName);
        return (rolePermissions & permission) == permission;
    }
}

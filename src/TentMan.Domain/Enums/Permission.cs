namespace TentMan.Domain.Enums;

/// <summary>
/// Defines the permission types that can be granted to roles.
/// Use bitwise operations for combining multiple permissions.
/// </summary>
[Flags]
public enum Permission
{
    /// <summary>
    /// No permissions.
    /// </summary>
    None = 0,

    /// <summary>
    /// Permission to read/view resources.
    /// </summary>
    Read = 1 << 0,  // 1

    /// <summary>
    /// Permission to create new resources.
    /// </summary>
    Create = 1 << 1,  // 2

    /// <summary>
    /// Permission to update/edit existing resources.
    /// </summary>
    Update = 1 << 2,  // 4

    /// <summary>
    /// Permission to delete resources.
    /// </summary>
    Delete = 1 << 3,  // 8

    /// <summary>
    /// Permission to manage user accounts.
    /// </summary>
    ManageUsers = 1 << 4,  // 16

    /// <summary>
    /// Permission to manage roles and permissions.
    /// </summary>
    ManageRoles = 1 << 5,  // 32

    /// <summary>
    /// Permission to access system configuration.
    /// </summary>
    SystemConfiguration = 1 << 6,  // 64

    /// <summary>
    /// Permission to view audit logs.
    /// </summary>
    ViewAuditLogs = 1 << 7,  // 128

    /// <summary>
    /// Permission to export data.
    /// </summary>
    ExportData = 1 << 8,  // 256

    /// <summary>
    /// Permission to import data.
    /// </summary>
    ImportData = 1 << 9,  // 512

    /// <summary>
    /// All permissions combined.
    /// </summary>
    All = Read | Create | Update | Delete | ManageUsers | ManageRoles | 
          SystemConfiguration | ViewAuditLogs | ExportData | ImportData
}

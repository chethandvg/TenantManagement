namespace TentMan.Domain.Enums;

/// <summary>
/// Defines the system-level roles available in the application.
/// These roles represent different levels of access and permissions.
/// </summary>
public enum SystemRole
{
    /// <summary>
    /// Guest role with minimal read-only access.
    /// </summary>
    Guest = 0,

    /// <summary>
    /// Standard user role with basic application access.
    /// </summary>
    User = 1,

    /// <summary>
    /// Manager role with elevated permissions for team management.
    /// </summary>
    Manager = 2,

    /// <summary>
    /// Administrator role with full system access and configuration rights.
    /// </summary>
    Administrator = 3,

    /// <summary>
    /// System administrator role with unrestricted access to all features.
    /// </summary>
    SuperAdmin = 4
}

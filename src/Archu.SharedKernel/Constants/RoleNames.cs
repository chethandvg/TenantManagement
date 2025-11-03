namespace Archu.SharedKernel.Constants;

/// <summary>
/// Contains constant values for system roles.
/// Use these constants instead of hard-coded strings for type safety and consistency.
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// Guest role with minimal read-only access.
    /// </summary>
    public const string Guest = "Guest";

    /// <summary>
    /// Standard user role with basic application access.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Manager role with elevated permissions for team management.
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// Administrator role with full system access.
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>
    /// System administrator with unrestricted access.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// Gets all standard role names.
    /// </summary>
    public static readonly string[] All = 
    [
        Guest,
        User,
        Manager,
        Administrator,
        SuperAdmin
    ];

    /// <summary>
    /// Gets role names that require administrative privileges.
    /// </summary>
    public static readonly string[] AdminRoles = 
    [
        Administrator,
        SuperAdmin
    ];

    /// <summary>
    /// Gets role names for managers and above.
    /// </summary>
    public static readonly string[] ManagerAndAbove = 
    [
        Manager,
        Administrator,
        SuperAdmin
    ];
}

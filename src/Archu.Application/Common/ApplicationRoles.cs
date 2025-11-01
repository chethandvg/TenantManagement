namespace Archu.Application.Common;

/// <summary>
/// Defines standard role names used throughout the application.
/// Use these constants instead of hardcoding role names for consistency and maintainability.
/// </summary>
public static class ApplicationRoles
{
    /// <summary>
    /// Administrator role with full system access.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Standard user role with basic access.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Manager role with elevated privileges.
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// Supervisor role for team oversight.
    /// </summary>
    public const string Supervisor = "Supervisor";

    /// <summary>
    /// Product manager role for product catalog management.
    /// </summary>
    public const string ProductManager = "ProductManager";

    /// <summary>
    /// Editor role for content modification.
    /// </summary>
    public const string Editor = "Editor";

    /// <summary>
    /// Viewer role with read-only access.
    /// </summary>
    public const string Viewer = "Viewer";

    /// <summary>
    /// Gets all defined role names.
    /// </summary>
    public static IEnumerable<string> All => new[]
    {
        Admin,
        User,
        Manager,
        Supervisor,
        ProductManager,
        Editor,
        Viewer
    };

    /// <summary>
    /// Gets roles that have administrative privileges.
    /// </summary>
    public static IEnumerable<string> Administrative => new[]
    {
        Admin,
        Manager
    };

    /// <summary>
    /// Gets roles that can manage products (create, update, delete).
    /// </summary>
    public static IEnumerable<string> ProductManagement => new[]
    {
        Admin,
        ProductManager,
        Editor
    };

    /// <summary>
    /// Gets roles that can approve orders or workflows.
    /// </summary>
    public static IEnumerable<string> Approvers => new[]
    {
        Admin,
        Manager,
        Supervisor
    };

    /// <summary>
    /// Gets roles that have read-only access.
    /// </summary>
    public static IEnumerable<string> ReadOnly => new[]
    {
        Viewer
    };

    /// <summary>
    /// Checks if a role name is valid (exists in the defined roles).
    /// </summary>
    /// <param name="roleName">The role name to validate.</param>
    /// <returns>True if the role exists; otherwise, false.</returns>
    public static bool IsValid(string roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) && All.Contains(roleName);
    }

    /// <summary>
    /// Checks if a role has administrative privileges.
    /// </summary>
    /// <param name="roleName">The role name to check.</param>
    /// <returns>True if the role has administrative privileges; otherwise, false.</returns>
    public static bool IsAdministrative(string roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) && Administrative.Contains(roleName);
    }
}

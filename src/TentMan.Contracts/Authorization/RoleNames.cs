namespace TentMan.Contracts.Authorization;

/// <summary>
/// Contains role name constants used in the application.
/// These are shared between the API and UI layers.
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// Administrator role name.
    /// </summary>
    public const string Administrator = "Admin";

    /// <summary>
    /// Manager role name.
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// User role name.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Tenant role name.
    /// </summary>
    public const string Tenant = "Tenant";

    /// <summary>
    /// Guest role name.
    /// </summary>
    public const string Guest = "Guest";

    /// <summary>
    /// SuperAdmin role name.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";
}

namespace TentMan.Contracts.Authorization;

/// <summary>
/// Contains authorization policy names used in the application.
/// These are shared between the API and UI layers.
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Policy for viewing the tenant portal.
    /// </summary>
    public const string CanViewTenantPortal = "CanViewTenantPortal";

    /// <summary>
    /// Policy for viewing property management features.
    /// </summary>
    public const string CanViewPropertyManagement = "CanViewPropertyManagement";

    /// <summary>
    /// Policy for viewing buildings.
    /// </summary>
    public const string CanViewBuildings = "CanViewBuildings";

    /// <summary>
    /// Policy for viewing tenants.
    /// </summary>
    public const string CanViewTenants = "CanViewTenants";

    /// <summary>
    /// Policy for viewing leases.
    /// </summary>
    public const string CanViewLeases = "CanViewLeases";
}

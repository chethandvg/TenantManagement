namespace TentMan.Api.Authorization;

/// <summary>
/// Contains constants for authorization policy names.
/// Use these constants instead of magic strings for type safety.
/// </summary>
public static class PolicyNames
{
    // Email & MFA policies
    public const string EmailVerified = "EmailVerified";
    public const string TwoFactorEnabled = "TwoFactorEnabled";

    // Role-based policies
    public const string RequireUserRole = "RequireUserRole";
    public const string RequireManagerRole = "RequireManagerRole";
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireSuperAdminRole = "RequireSuperAdminRole";

    // Resource ownership policy
    public const string ResourceOwner = "ResourceOwner";

    // Product policies
    public static class Products
    {
        public const string View = "Products.View";
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
    }
}

namespace Archu.Api.Authorization;

/// <summary>
/// Defines all authorization policy names used in the application.
/// Centralized policy names ensure consistency across the application.
/// </summary>
public static class AuthorizationPolicies
{
    // ============================================
    // ROLE-BASED POLICIES
    // ============================================

    /// <summary>
    /// Policy that requires the user to be an Administrator.
    /// Grants full access to all resources.
    /// </summary>
    public const string RequireAdminRole = "RequireAdminRole";

    /// <summary>
    /// Policy that requires the user to be a Manager.
    /// Grants elevated access for managing resources.
    /// </summary>
    public const string RequireManagerRole = "RequireManagerRole";

    /// <summary>
    /// Policy that requires the user to be a regular User.
    /// Standard access for authenticated users.
    /// </summary>
    public const string RequireUserRole = "RequireUserRole";

    /// <summary>
    /// Policy that requires the user to be either Admin or Manager.
    /// Used for operations that require elevated permissions.
    /// </summary>
    public const string RequireAdminOrManager = "RequireAdminOrManager";

    // ============================================
    // CLAIM-BASED POLICIES
    // ============================================

    /// <summary>
    /// Policy that requires a verified email address.
    /// Used for operations that require email confirmation.
    /// </summary>
    public const string RequireEmailVerified = "RequireEmailVerified";

    /// <summary>
    /// Policy that requires Two-Factor Authentication to be enabled.
    /// Enhanced security for sensitive operations.
    /// </summary>
    public const string RequireTwoFactorEnabled = "RequireTwoFactorEnabled";

    // ============================================
    // PERMISSION-BASED POLICIES (RESOURCE-SPECIFIC)
    // ============================================

    /// <summary>
    /// Policy for creating products.
    /// Requires 'products:create' permission.
    /// </summary>
    public const string CanCreateProducts = "CanCreateProducts";

    /// <summary>
    /// Policy for updating products.
    /// Requires 'products:update' permission.
    /// </summary>
    public const string CanUpdateProducts = "CanUpdateProducts";

    /// <summary>
    /// Policy for deleting products.
    /// Requires 'products:delete' permission.
    /// </summary>
    public const string CanDeleteProducts = "CanDeleteProducts";

    /// <summary>
    /// Policy for reading products.
    /// Requires 'products:read' permission.
    /// </summary>
    public const string CanReadProducts = "CanReadProducts";

    /// <summary>
    /// Policy for managing all products (CRUD).
    /// Requires 'products:manage' permission or Admin role.
    /// </summary>
    public const string CanManageProducts = "CanManageProducts";

    // ============================================
    // COMPOSITE POLICIES
    // ============================================

    /// <summary>
    /// Policy that requires authenticated user with verified email.
    /// Basic requirement for most operations.
    /// </summary>
    public const string AuthenticatedWithVerifiedEmail = "AuthenticatedWithVerifiedEmail";

    /// <summary>
    /// Policy for accessing API documentation.
    /// Restricted to Admin and Manager roles.
    /// </summary>
    public const string CanAccessApiDocs = "CanAccessApiDocs";
}

/// <summary>
/// Defines standard role names used in the application.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
}

/// <summary>
/// Defines permission claim types used in the application.
/// Permissions follow the pattern: {resource}:{action}
/// </summary>
public static class Permissions
{
    // Products permissions
    public const string ProductsCreate = "products:create";
    public const string ProductsRead = "products:read";
    public const string ProductsUpdate = "products:update";
    public const string ProductsDelete = "products:delete";
    public const string ProductsManage = "products:manage";

    // Users permissions (for user management)
    public const string UsersCreate = "users:create";
    public const string UsersRead = "users:read";
    public const string UsersUpdate = "users:update";
    public const string UsersDelete = "users:delete";
    public const string UsersManage = "users:manage";

    // Roles permissions (for role management)
    public const string RolesCreate = "roles:create";
    public const string RolesRead = "roles:read";
    public const string RolesUpdate = "roles:update";
    public const string RolesDelete = "roles:delete";
    public const string RolesManage = "roles:manage";
}

/// <summary>
/// Defines custom claim types used in the application.
/// </summary>
public static class CustomClaimTypes
{
    public const string Permission = "permission";
    public const string EmailVerified = "email_verified";
    public const string TwoFactorEnabled = "two_factor_enabled";
    public const string Department = "department";
    public const string EmployeeId = "employee_id";
}

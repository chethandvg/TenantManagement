namespace TentMan.Shared.Constants.Authorization;

/// <summary>
/// Contains role name constants shared across all application layers.
/// These values must match the actual roles configured in the identity system.
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// Administrator role with full system access.
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>
    /// Manager role with elevated permissions for team management.
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// Standard user role with basic application access.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Tenant role for tenant portal users.
    /// </summary>
    public const string Tenant = "Tenant";

    /// <summary>
    /// Guest role with minimal read-only access.
    /// </summary>
    public const string Guest = "Guest";

    /// <summary>
    /// System administrator with unrestricted access.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";
}

/// <summary>
/// Contains authorization policy name constants shared across all application layers.
/// </summary>
public static class PolicyNames
{
    // UI Navigation policies
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

    // Email & MFA policies
    /// <summary>
    /// Policy requiring email verification.
    /// </summary>
    public const string EmailVerified = "EmailVerified";

    /// <summary>
    /// Policy requiring two-factor authentication.
    /// </summary>
    public const string TwoFactorEnabled = "TwoFactorEnabled";

    // Role-based policies
    /// <summary>
    /// Policy requiring User role.
    /// </summary>
    public const string RequireUserRole = "RequireUserRole";

    /// <summary>
    /// Policy requiring Manager role.
    /// </summary>
    public const string RequireManagerRole = "RequireManagerRole";

    /// <summary>
    /// Policy requiring Administrator role.
    /// </summary>
    public const string RequireAdminRole = "RequireAdminRole";

    /// <summary>
    /// Policy requiring SuperAdmin role.
    /// </summary>
    public const string RequireSuperAdminRole = "RequireSuperAdminRole";

    /// <summary>
    /// Policy requiring Tenant role.
    /// </summary>
    public const string RequireTenantRole = "RequireTenantRole";

    // Resource ownership policy
    /// <summary>
    /// Policy requiring resource ownership.
    /// </summary>
    public const string ResourceOwner = "ResourceOwner";

    // Lease access policy (for tenants and admins)
    /// <summary>
    /// Policy for lease access (tenants and admins).
    /// </summary>
    public const string LeaseAccess = "LeaseAccess";

    // Product policies
    /// <summary>
    /// Product-related policy names.
    /// </summary>
    public static class Products
    {
        /// <summary>
        /// Policy for viewing products.
        /// </summary>
        public const string View = "Products.View";

        /// <summary>
        /// Policy for creating products.
        /// </summary>
        public const string Create = "Products.Create";

        /// <summary>
        /// Policy for updating products.
        /// </summary>
        public const string Update = "Products.Update";

        /// <summary>
        /// Policy for deleting products.
        /// </summary>
        public const string Delete = "Products.Delete";
    }
}

/// <summary>
/// Contains custom claim type constants shared across all application layers.
/// </summary>
public static class ClaimTypes
{
    /// <summary>
    /// Claim type for permissions.
    /// </summary>
    public const string Permission = "permission";

    /// <summary>
    /// Claim type for email verification status.
    /// </summary>
    public const string EmailVerified = "email_verified";

    /// <summary>
    /// Claim type for two-factor authentication status.
    /// </summary>
    public const string TwoFactorEnabled = "two_factor_enabled";

    /// <summary>
    /// Claim type for department.
    /// </summary>
    public const string Department = "department";

    /// <summary>
    /// Claim type for employee ID.
    /// </summary>
    public const string EmployeeId = "employee_id";
}

/// <summary>
/// Contains permission value constants shared across all application layers.
/// Permissions follow the pattern: {resource}:{action}
/// </summary>
public static class PermissionValues
{
    /// <summary>
    /// Tenant Portal permissions.
    /// </summary>
    public static class TenantPortal
    {
        /// <summary>
        /// Permission to view the tenant portal.
        /// </summary>
        public const string View = "tenantportal:view";
    }

    /// <summary>
    /// Property Management permissions.
    /// </summary>
    public static class PropertyManagement
    {
        /// <summary>
        /// Permission to view property management features.
        /// </summary>
        public const string View = "propertymanagement:view";
    }

    /// <summary>
    /// Building resource permissions.
    /// </summary>
    public static class Buildings
    {
        /// <summary>
        /// Permission to read/view buildings.
        /// </summary>
        public const string Read = "buildings:read";

        /// <summary>
        /// Permission to create new buildings.
        /// </summary>
        public const string Create = "buildings:create";

        /// <summary>
        /// Permission to update existing buildings.
        /// </summary>
        public const string Update = "buildings:update";

        /// <summary>
        /// Permission to delete buildings.
        /// </summary>
        public const string Delete = "buildings:delete";

        /// <summary>
        /// Permission to manage all building operations (CRUD).
        /// </summary>
        public const string Manage = "buildings:manage";
    }

    /// <summary>
    /// Tenant resource permissions.
    /// </summary>
    public static class Tenants
    {
        /// <summary>
        /// Permission to read/view tenants.
        /// </summary>
        public const string Read = "tenants:read";

        /// <summary>
        /// Permission to create new tenants.
        /// </summary>
        public const string Create = "tenants:create";

        /// <summary>
        /// Permission to update existing tenants.
        /// </summary>
        public const string Update = "tenants:update";

        /// <summary>
        /// Permission to delete tenants.
        /// </summary>
        public const string Delete = "tenants:delete";

        /// <summary>
        /// Permission to manage all tenant operations (CRUD).
        /// </summary>
        public const string Manage = "tenants:manage";
    }

    /// <summary>
    /// Lease resource permissions.
    /// </summary>
    public static class Leases
    {
        /// <summary>
        /// Permission to read/view leases.
        /// </summary>
        public const string Read = "leases:read";

        /// <summary>
        /// Permission to create new leases.
        /// </summary>
        public const string Create = "leases:create";

        /// <summary>
        /// Permission to update existing leases.
        /// </summary>
        public const string Update = "leases:update";

        /// <summary>
        /// Permission to delete leases.
        /// </summary>
        public const string Delete = "leases:delete";

        /// <summary>
        /// Permission to manage all lease operations (CRUD).
        /// </summary>
        public const string Manage = "leases:manage";
    }

    /// <summary>
    /// Product resource permissions.
    /// </summary>
    public static class Products
    {
        /// <summary>
        /// Permission to read/view products.
        /// </summary>
        public const string Read = "products:read";

        /// <summary>
        /// Permission to create new products.
        /// </summary>
        public const string Create = "products:create";

        /// <summary>
        /// Permission to update existing products.
        /// </summary>
        public const string Update = "products:update";

        /// <summary>
        /// Permission to delete products.
        /// </summary>
        public const string Delete = "products:delete";

        /// <summary>
        /// Permission to manage all product operations (CRUD).
        /// </summary>
        public const string Manage = "products:manage";
    }

    /// <summary>
    /// User resource permissions.
    /// </summary>
    public static class Users
    {
        /// <summary>
        /// Permission to read/view users.
        /// </summary>
        public const string Read = "users:read";

        /// <summary>
        /// Permission to create new users.
        /// </summary>
        public const string Create = "users:create";

        /// <summary>
        /// Permission to update existing users.
        /// </summary>
        public const string Update = "users:update";

        /// <summary>
        /// Permission to delete users.
        /// </summary>
        public const string Delete = "users:delete";

        /// <summary>
        /// Permission to manage all user operations (CRUD).
        /// </summary>
        public const string Manage = "users:manage";
    }

    /// <summary>
    /// Role resource permissions.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Permission to read/view roles.
        /// </summary>
        public const string Read = "roles:read";

        /// <summary>
        /// Permission to create new roles.
        /// </summary>
        public const string Create = "roles:create";

        /// <summary>
        /// Permission to update existing roles.
        /// </summary>
        public const string Update = "roles:update";

        /// <summary>
        /// Permission to delete roles.
        /// </summary>
        public const string Delete = "roles:delete";

        /// <summary>
        /// Permission to manage all role operations (CRUD).
        /// </summary>
        public const string Manage = "roles:manage";
    }
}

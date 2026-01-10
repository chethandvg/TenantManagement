namespace TentMan.Domain.Constants;

/// <summary>
/// Defines permission claim values used throughout the application.
/// Permissions follow the pattern: {resource}:{action}
/// </summary>
public static class PermissionNames
{
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
    /// User management permissions.
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
    /// Role management permissions.
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
    /// Gets all product permissions as an array.
    /// </summary>
    public static string[] GetAllProductPermissions() => new[]
    {
        Products.Read,
        Products.Create,
        Products.Update,
        Products.Delete,
        Products.Manage
    };

    /// <summary>
    /// Gets all user permissions as an array.
    /// </summary>
    public static string[] GetAllUserPermissions() => new[]
    {
        Users.Read,
        Users.Create,
        Users.Update,
        Users.Delete,
        Users.Manage
    };

    /// <summary>
    /// Gets all role permissions as an array.
    /// </summary>
    public static string[] GetAllRolePermissions() => new[]
    {
        Roles.Read,
        Roles.Create,
        Roles.Update,
        Roles.Delete,
        Roles.Manage
    };

    /// <summary>
    /// Gets all building permissions as an array.
    /// </summary>
    public static string[] GetAllBuildingPermissions() => new[]
    {
        Buildings.Read,
        Buildings.Create,
        Buildings.Update,
        Buildings.Delete,
        Buildings.Manage
    };

    /// <summary>
    /// Gets all tenant permissions as an array.
    /// </summary>
    public static string[] GetAllTenantPermissions() => new[]
    {
        Tenants.Read,
        Tenants.Create,
        Tenants.Update,
        Tenants.Delete,
        Tenants.Manage
    };

    /// <summary>
    /// Gets all lease permissions as an array.
    /// </summary>
    public static string[] GetAllLeasePermissions() => new[]
    {
        Leases.Read,
        Leases.Create,
        Leases.Update,
        Leases.Delete,
        Leases.Manage
    };

    /// <summary>
    /// Gets all permissions in the system.
    /// </summary>
    public static string[] GetAllPermissions()
    {
        return GetAllProductPermissions()
            .Concat(GetAllUserPermissions())
            .Concat(GetAllRolePermissions())
            .Concat(GetAllBuildingPermissions())
            .Concat(GetAllTenantPermissions())
            .Concat(GetAllLeasePermissions())
            .Concat(new[] { TenantPortal.View, PropertyManagement.View })
            .ToArray();
    }
}

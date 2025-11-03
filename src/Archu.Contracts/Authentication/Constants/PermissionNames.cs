namespace Archu.Contracts.Authentication.Constants;

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
    /// Gets all permissions in the system.
    /// </summary>
    public static string[] GetAllPermissions()
    {
        return GetAllProductPermissions()
            .Concat(GetAllUserPermissions())
            .Concat(GetAllRolePermissions())
            .ToArray();
    }
}

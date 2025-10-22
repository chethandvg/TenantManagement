namespace Archu.AdminApi.Authorization;

/// <summary>
/// Contains constants for Admin API authorization policy names.
/// Use these constants instead of magic strings for type safety and consistency.
/// </summary>
public static class AdminPolicyNames
{
    /// <summary>
    /// Role management policies.
    /// Controls access to role CRUD operations.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Policy for viewing/reading roles.
        /// Required Roles: SuperAdmin, Administrator, Manager
        /// </summary>
        public const string View = "Admin.Roles.View";

        /// <summary>
        /// Policy for creating new roles.
        /// Required Roles: SuperAdmin, Administrator
        /// </summary>
        public const string Create = "Admin.Roles.Create";

        /// <summary>
        /// Policy for updating existing roles.
        /// Required Roles: SuperAdmin, Administrator
        /// </summary>
        public const string Update = "Admin.Roles.Update";

        /// <summary>
        /// Policy for deleting roles.
        /// Required Roles: SuperAdmin (only)
        /// </summary>
        public const string Delete = "Admin.Roles.Delete";

        /// <summary>
        /// Policy for managing all role operations.
        /// Required Roles: SuperAdmin
        /// </summary>
        public const string Manage = "Admin.Roles.Manage";
    }

    /// <summary>
    /// User management policies.
    /// Controls access to user CRUD operations.
    /// </summary>
    public static class Users
    {
        /// <summary>
        /// Policy for viewing/reading users.
        /// Required Roles: SuperAdmin, Administrator, Manager
        /// </summary>
        public const string View = "Admin.Users.View";

        /// <summary>
        /// Policy for creating new users.
        /// Required Roles: SuperAdmin, Administrator, Manager
        /// </summary>
        public const string Create = "Admin.Users.Create";

        /// <summary>
        /// Policy for updating existing users.
        /// Required Roles: SuperAdmin, Administrator, Manager
        /// </summary>
        public const string Update = "Admin.Users.Update";

        /// <summary>
        /// Policy for deleting users.
        /// Required Roles: SuperAdmin, Administrator
        /// </summary>
        public const string Delete = "Admin.Users.Delete";

        /// <summary>
        /// Policy for managing all user operations.
        /// Required Roles: SuperAdmin, Administrator
        /// </summary>
        public const string Manage = "Admin.Users.Manage";
    }

    /// <summary>
    /// User-Role assignment policies.
    /// Controls access to role assignment operations.
    /// </summary>
    public static class UserRoles
    {
        /// <summary>
        /// Policy for viewing user role assignments.
        /// Required Roles: SuperAdmin, Administrator, Manager
        /// </summary>
        public const string View = "Admin.UserRoles.View";

        /// <summary>
        /// Policy for assigning roles to users.
        /// Required Roles: SuperAdmin, Administrator
        /// </summary>
        public const string Assign = "Admin.UserRoles.Assign";

        /// <summary>
        /// Policy for removing roles from users.
        /// Required Roles: SuperAdmin, Administrator
        /// </summary>
        public const string Remove = "Admin.UserRoles.Remove";

        /// <summary>
        /// Policy for managing all user-role operations.
        /// Required Roles: SuperAdmin
        /// </summary>
        public const string Manage = "Admin.UserRoles.Manage";
    }

    /// <summary>
    /// Base admin access policy.
    /// Minimum requirement for accessing any admin endpoint.
    /// </summary>
    public const string RequireAdminAccess = "Admin.RequireAdminAccess";
}

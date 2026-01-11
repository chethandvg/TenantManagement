using TentMan.AdminApi.Authorization.Requirements;
using TentMan.Shared.Constants.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace TentMan.AdminApi.Authorization;

/// <summary>
/// Extension methods for configuring Admin API authorization policies.
/// </summary>
public static class AdminAuthorizationPolicyExtensions
{
    /// <summary>
    /// Configures all Admin API authorization policies.
    /// </summary>
    public static void ConfigureAdminPolicies(this AuthorizationOptions options)
    {
        // Base admin access policy - minimum requirement for any admin endpoint
        options.AddPolicy(AdminPolicyNames.RequireAdminAccess, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(RoleNames.SuperAdmin, RoleNames.Administrator, RoleNames.Manager);
        });

        // Configure role management policies
        ConfigureRolePolicies(options);

        // Configure user management policies
        ConfigureUserPolicies(options);

        // Configure user-role assignment policies
        ConfigureUserRolePolicies(options);
    }

    /// <summary>
    /// Configures authorization policies for role management operations.
    /// </summary>
    private static void ConfigureRolePolicies(AuthorizationOptions options)
    {
        // View roles - accessible by SuperAdmin, Administrator, and Manager
        options.AddPolicy(AdminPolicyNames.Roles.View, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "View Roles",
                RoleNames.SuperAdmin,
                RoleNames.Administrator,
                RoleNames.Manager));
        });

        // Create roles - accessible by SuperAdmin and Administrator
        options.AddPolicy(AdminPolicyNames.Roles.Create, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Create Role",
                RoleNames.SuperAdmin,
                RoleNames.Administrator));
        });

        // Update roles - accessible by SuperAdmin and Administrator
        options.AddPolicy(AdminPolicyNames.Roles.Update, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Update Role",
                RoleNames.SuperAdmin,
                RoleNames.Administrator));
        });

        // Delete roles - accessible by SuperAdmin only
        options.AddPolicy(AdminPolicyNames.Roles.Delete, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Delete Role",
                RoleNames.SuperAdmin));
        });

        // Manage roles - accessible by SuperAdmin only
        options.AddPolicy(AdminPolicyNames.Roles.Manage, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Manage Roles",
                RoleNames.SuperAdmin));
        });
    }

    /// <summary>
    /// Configures authorization policies for user management operations.
    /// </summary>
    private static void ConfigureUserPolicies(AuthorizationOptions options)
    {
        // View users - accessible by SuperAdmin, Administrator, and Manager
        options.AddPolicy(AdminPolicyNames.Users.View, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "View Users",
                RoleNames.SuperAdmin,
                RoleNames.Administrator,
                RoleNames.Manager));
        });

        // Create users - accessible by SuperAdmin, Administrator, and Manager
        options.AddPolicy(AdminPolicyNames.Users.Create, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Create User",
                RoleNames.SuperAdmin,
                RoleNames.Administrator,
                RoleNames.Manager));
        });

        // Update users - accessible by SuperAdmin, Administrator, and Manager
        options.AddPolicy(AdminPolicyNames.Users.Update, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Update User",
                RoleNames.SuperAdmin,
                RoleNames.Administrator,
                RoleNames.Manager));
        });

        // Delete users - accessible by SuperAdmin and Administrator
        options.AddPolicy(AdminPolicyNames.Users.Delete, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Delete User",
                RoleNames.SuperAdmin,
                RoleNames.Administrator));
        });

        // Manage users - accessible by SuperAdmin and Administrator
        options.AddPolicy(AdminPolicyNames.Users.Manage, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Manage Users",
                RoleNames.SuperAdmin,
                RoleNames.Administrator));
        });
    }

    /// <summary>
    /// Configures authorization policies for user-role assignment operations.
    /// </summary>
    private static void ConfigureUserRolePolicies(AuthorizationOptions options)
    {
        // View user roles - accessible by SuperAdmin, Administrator, and Manager
        options.AddPolicy(AdminPolicyNames.UserRoles.View, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "View User Roles",
                RoleNames.SuperAdmin,
                RoleNames.Administrator,
                RoleNames.Manager));
        });

        // Assign roles - accessible by SuperAdmin and Administrator
        options.AddPolicy(AdminPolicyNames.UserRoles.Assign, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Assign Role to User",
                RoleNames.SuperAdmin,
                RoleNames.Administrator));
        });

        // Remove roles - accessible by SuperAdmin and Administrator
        options.AddPolicy(AdminPolicyNames.UserRoles.Remove, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Remove Role from User",
                RoleNames.SuperAdmin,
                RoleNames.Administrator));
        });

        // Manage user roles - accessible by SuperAdmin only
        options.AddPolicy(AdminPolicyNames.UserRoles.Manage, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new AdminRoleRequirement(
                "Manage User Roles",
                RoleNames.SuperAdmin));
        });
    }
}

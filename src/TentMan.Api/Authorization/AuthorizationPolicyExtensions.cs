using TentMan.Api.Authorization.Requirements;
using TentMan.Shared.Constants.Authorization;
using Microsoft.AspNetCore.Authorization;

using SharedClaimTypes = TentMan.Shared.Constants.Authorization.ClaimTypes;
using SharedPermissionValues = TentMan.Shared.Constants.Authorization.PermissionValues;

namespace TentMan.Api.Authorization;

/// <summary>
/// Extension methods for configuring TentMan authorization policies.
/// </summary>
public static class AuthorizationPolicyExtensions
{
    /// <summary>
    /// Configures all TentMan authorization policies.
    /// </summary>
    public static void ConfigureTentManPolicies(this AuthorizationOptions options)
    {
        // Email verification policy
        options.AddPolicy(PolicyNames.EmailVerified, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new EmailVerifiedRequirement());
        });

        // Two-factor authentication policy
        options.AddPolicy(PolicyNames.TwoFactorEnabled, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new TwoFactorEnabledRequirement());
        });

        // Role-based policies
        options.AddPolicy(PolicyNames.RequireUserRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.User));
        });

        options.AddPolicy(PolicyNames.RequireManagerRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.Manager));
        });

        options.AddPolicy(PolicyNames.RequireAdminRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.Administrator));
        });

        options.AddPolicy(PolicyNames.RequireTenantRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.Tenant));
        });

        options.AddPolicy(PolicyNames.RequireSuperAdminRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.SuperAdmin));
        });

        // Resource ownership policy
        options.AddPolicy(PolicyNames.ResourceOwner, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new ResourceOwnerRequirement());
        });

        // Lease access policy
        options.AddPolicy(PolicyNames.LeaseAccess, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new LeaseAccessRequirement());
        });

        // Permission-based policies for Products
        ConfigureProductPolicies(options);

        // Permission-based policies for Navigation/UI
        ConfigureNavigationPolicies(options);

        // Permission-based policies for Buildings, Tenants, Leases
        ConfigurePropertyManagementPolicies(options);
    }

    /// <summary>
    /// Configures permission-based authorization policies for navigation and UI elements.
    /// </summary>
    private static void ConfigureNavigationPolicies(AuthorizationOptions options)
    {
        // Tenant Portal policy - allow Tenant role or explicit permission
        options.AddPolicy(PolicyNames.CanViewTenantPortal, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole(RoleNames.Tenant) ||
                context.User.HasClaim(c => c.Type == SharedClaimTypes.Permission &&
                                           c.Value == SharedPermissionValues.TenantPortal.View));
        });

        // Property Management policy
        options.AddPolicy(PolicyNames.CanViewPropertyManagement, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole(RoleNames.Administrator) ||
                context.User.IsInRole(RoleNames.Manager) ||
                context.User.IsInRole(RoleNames.User) ||
                context.User.HasClaim(c => c.Type == SharedClaimTypes.Permission &&
                                           c.Value == SharedPermissionValues.PropertyManagement.View));
        });
    }

    /// <summary>
    /// Configures permission-based authorization policies for property management operations.
    /// </summary>
    private static void ConfigurePropertyManagementPolicies(AuthorizationOptions options)
    {
        // Buildings policies
        options.AddPolicy(PolicyNames.CanViewBuildings, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole(RoleNames.Administrator) ||
                context.User.IsInRole(RoleNames.Manager) ||
                context.User.IsInRole(RoleNames.User) ||
                context.User.HasClaim(c => c.Type == SharedClaimTypes.Permission &&
                                           c.Value == SharedPermissionValues.Buildings.Read));
        });

        // Tenants policies
        options.AddPolicy(PolicyNames.CanViewTenants, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole(RoleNames.Administrator) ||
                context.User.IsInRole(RoleNames.Manager) ||
                context.User.IsInRole(RoleNames.User) ||
                context.User.HasClaim(c => c.Type == SharedClaimTypes.Permission &&
                                           c.Value == SharedPermissionValues.Tenants.Read));
        });

        // Leases policies
        options.AddPolicy(PolicyNames.CanViewLeases, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole(RoleNames.Administrator) ||
                context.User.IsInRole(RoleNames.Manager) ||
                context.User.IsInRole(RoleNames.User) ||
                context.User.HasClaim(c => c.Type == SharedClaimTypes.Permission &&
                                           c.Value == SharedPermissionValues.Leases.Read));
        });
    }

    /// <summary>
    /// Configures permission-based authorization policies for product operations.
    /// Uses strongly-typed permission constants from Shared layer.
    /// </summary>
    private static void ConfigureProductPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(PolicyNames.Products.View, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(SharedPermissionValues.Products.Read));
        });

        options.AddPolicy(PolicyNames.Products.Create, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(SharedPermissionValues.Products.Create));
        });

        options.AddPolicy(PolicyNames.Products.Update, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(SharedPermissionValues.Products.Update));
        });

        options.AddPolicy(PolicyNames.Products.Delete, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(SharedPermissionValues.Products.Delete));
        });
    }
}

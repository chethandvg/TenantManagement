using Archu.Api.Authorization.Requirements;
using Archu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Archu.Api.Authorization;

/// <summary>
/// Extension methods for configuring Archu authorization policies.
/// </summary>
public static class AuthorizationPolicyExtensions
{
    /// <summary>
    /// Configures all Archu authorization policies.
    /// </summary>
    public static void ConfigureArchuPolicies(this AuthorizationOptions options)
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

        // Permission-based policies for Products
        ConfigureProductPolicies(options);
    }

    /// <summary>
    /// Configures permission-based authorization policies for product operations.
    /// Uses strongly-typed permission constants from Domain layer.
    /// </summary>
    private static void ConfigureProductPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(PolicyNames.Products.View, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Read));
        });

        options.AddPolicy(PolicyNames.Products.Create, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Create));
        });

        options.AddPolicy(PolicyNames.Products.Update, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Update));
        });

        options.AddPolicy(PolicyNames.Products.Delete, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Delete));
        });
    }
}

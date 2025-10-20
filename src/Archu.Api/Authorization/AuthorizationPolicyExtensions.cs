using Archu.Api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Archu.Api.Authorization;

/// <summary>
/// Extension methods for configuring authorization policies.
/// </summary>
public static class AuthorizationPolicyExtensions
{
    /// <summary>
    /// Configures all authorization policies for the application.
    /// </summary>
    /// <param name="options">Authorization options to configure.</param>
    public static void ConfigureArchuPolicies(this AuthorizationOptions options)
    {
        // ============================================
        // ROLE-BASED POLICIES
        // ============================================

        options.AddPolicy(AuthorizationPolicies.RequireAdminRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(Roles.Admin);
        });

        options.AddPolicy(AuthorizationPolicies.RequireManagerRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(Roles.Manager);
        });

        options.AddPolicy(AuthorizationPolicies.RequireUserRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(Roles.User);
        });

        options.AddPolicy(AuthorizationPolicies.RequireAdminOrManager, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new MinimumRoleRequirement(Roles.Admin, Roles.Manager));
        });

        // ============================================
        // CLAIM-BASED POLICIES
        // ============================================

        options.AddPolicy(AuthorizationPolicies.RequireEmailVerified, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new EmailVerifiedRequirement());
        });

        options.AddPolicy(AuthorizationPolicies.RequireTwoFactorEnabled, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new TwoFactorEnabledRequirement());
        });

        // ============================================
        // PERMISSION-BASED POLICIES
        // ============================================

        options.AddPolicy(AuthorizationPolicies.CanCreateProducts, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(Permissions.ProductsCreate));
        });

        options.AddPolicy(AuthorizationPolicies.CanReadProducts, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(Permissions.ProductsRead));
        });

        options.AddPolicy(AuthorizationPolicies.CanUpdateProducts, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(Permissions.ProductsUpdate));
        });

        options.AddPolicy(AuthorizationPolicies.CanDeleteProducts, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(Permissions.ProductsDelete));
        });

        options.AddPolicy(AuthorizationPolicies.CanManageProducts, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                // User can manage products if they have:
                // 1. Admin role, OR
                // 2. products:manage permission
                return context.User.IsInRole(Roles.Admin) ||
                       context.User.HasClaim(c =>
                           c.Type == CustomClaimTypes.Permission &&
                           c.Value == Permissions.ProductsManage);
            });
        });

        // ============================================
        // COMPOSITE POLICIES
        // ============================================

        options.AddPolicy(AuthorizationPolicies.AuthenticatedWithVerifiedEmail, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new EmailVerifiedRequirement());
        });

        options.AddPolicy(AuthorizationPolicies.CanAccessApiDocs, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new MinimumRoleRequirement(Roles.Admin, Roles.Manager));
        });

        // ============================================
        // FALLBACK POLICY (Optional)
        // ============================================
        // Uncomment to require authentication by default for all endpoints
        // options.FallbackPolicy = new AuthorizationPolicyBuilder()
        //     .RequireAuthenticatedUser()
        //     .Build();
    }

    /// <summary>
    /// Registers all authorization requirement handlers.
    /// </summary>
    /// <param name="services">Service collection.</param>
    public static IServiceCollection AddAuthorizationHandlers(this IServiceCollection services)
    {
        // Register custom authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, EmailVerifiedRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, TwoFactorEnabledRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, MinimumRoleRequirementHandler>();

        return services;
    }
}

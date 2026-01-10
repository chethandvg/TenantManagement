using TentMan.Api.Authorization.Handlers;
using TentMan.Api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace TentMan.Api.Authorization;

/// <summary>
/// Extension methods for registering authorization handlers.
/// </summary>
public static class AuthorizationHandlerExtensions
{
    /// <summary>
    /// Registers all custom authorization handlers for the application.
    /// </summary>
    public static IServiceCollection AddAuthorizationHandlers(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, EmailVerifiedRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, TwoFactorEnabledRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, MinimumRoleRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, ResourceOwnerRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, LeaseAccessRequirementHandler>();

        return services;
    }
}

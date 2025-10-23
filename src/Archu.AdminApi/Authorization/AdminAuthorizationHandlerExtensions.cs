using Archu.AdminApi.Authorization.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace Archu.AdminApi.Authorization;

/// <summary>
/// Extension methods for registering Admin API authorization handlers.
/// </summary>
public static class AdminAuthorizationHandlerExtensions
{
    /// <summary>
    /// Registers all custom authorization handlers for the Admin API.
    /// </summary>
    public static IServiceCollection AddAdminAuthorizationHandlers(this IServiceCollection services)
    {
        // Register admin authorization handlers
        services.AddScoped<IAuthorizationHandler, AdminRoleRequirementHandler>();

        return services;
    }
}

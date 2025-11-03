using System.Security.Claims;

namespace Archu.Ui.Services;

/// <summary>
/// Service for checking user permissions and roles in the UI.
/// Provides instant visibility into what to show/hide without hitting the API.
/// </summary>
public interface IUiAuthorizationService
{
    /// <summary>
    /// Gets the current authenticated user's ClaimsPrincipal.
    /// </summary>
    Task<ClaimsPrincipal?> GetCurrentUserAsync();

    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check (e.g., "products:delete").</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    Task<bool> HasPermissionAsync(string permission);

    /// <summary>
    /// Checks if the current user has any of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has at least one of the permissions; otherwise, false.</returns>
    Task<bool> HasAnyPermissionAsync(params string[] permissions);

    /// <summary>
    /// Checks if the current user has all of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has all of the permissions; otherwise, false.</returns>
    Task<bool> HasAllPermissionsAsync(params string[] permissions);

    /// <summary>
    /// Checks if the current user has a specific role.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns>True if the user has the role; otherwise, false.</returns>
    Task<bool> HasRoleAsync(string role);

    /// <summary>
    /// Checks if the current user has any of the specified roles.
    /// </summary>
    /// <param name="roles">The role names to check.</param>
    /// <returns>True if the user has at least one of the roles; otherwise, false.</returns>
    Task<bool> HasAnyRoleAsync(params string[] roles);

    /// <summary>
    /// Checks if the current user has all of the specified roles.
    /// </summary>
    /// <param name="roles">The role names to check.</param>
    /// <returns>True if the user has all of the roles; otherwise, false.</returns>
    Task<bool> HasAllRolesAsync(params string[] roles);
}

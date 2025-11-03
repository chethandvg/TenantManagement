using System.Security.Claims;
using Archu.SharedKernel.Constants;

namespace Archu.ApiClient.Authentication.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to check permissions and roles in Blazor UI.
/// These methods read claims from the current authenticated user context without hitting the API.
/// </summary>
public static class ClaimsPrincipalAuthorizationExtensions
{
    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="permission">The permission to check (e.g., "products:delete").</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(permission))
            return false;

        return principal.Claims.Any(c =>
            c.Type.Equals(CustomClaimTypes.Permission, StringComparison.Ordinal) &&
            c.Value.Equals(permission, StringComparison.Ordinal));
    }

    /// <summary>
    /// Checks if the current user has any of the specified permissions.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has at least one of the permissions; otherwise, false.</returns>
    public static bool HasAnyPermission(this ClaimsPrincipal principal, params string[] permissions)
    {
        if (principal?.Identity?.IsAuthenticated != true || permissions is null || permissions.Length == 0)
            return false;

        var userPermissions = principal.GetPermissions().ToHashSet(StringComparer.Ordinal);
        return permissions.Any(p => !string.IsNullOrWhiteSpace(p) && userPermissions.Contains(p));
    }

    /// <summary>
    /// Checks if the current user has all of the specified permissions.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the user has all of the permissions; otherwise, false.</returns>
    public static bool HasAllPermissions(this ClaimsPrincipal principal, params string[] permissions)
    {
        if (principal?.Identity?.IsAuthenticated != true || permissions is null || permissions.Length == 0)
            return false;

        var userPermissions = principal.GetPermissions().ToHashSet(StringComparer.Ordinal);
        return permissions.All(p => !string.IsNullOrWhiteSpace(p) && userPermissions.Contains(p));
    }

    /// <summary>
    /// Gets all permissions assigned to the current user.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>A collection of permission strings.</returns>
    public static IEnumerable<string> GetPermissions(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return Enumerable.Empty<string>();

        return principal.Claims
            .Where(c => c.Type.Equals(CustomClaimTypes.Permission, StringComparison.Ordinal))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Ordinal);
    }

    /// <summary>
    /// Checks if the user has a specific role.
    /// Case-insensitive comparison.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="role">The role name to check.</param>
    /// <returns>True if the user has the role; otherwise, false.</returns>
    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(role))
            return false;

        var roleClaimTypes = new[]
        {
            ClaimTypes.Role,
            "role",
            "roles",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };

        return principal.Claims
            .Where(c => roleClaimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            .Any(c => c.Value.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the user has any of the specified roles.
    /// Case-insensitive comparison.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="roles">The role names to check.</param>
    /// <returns>True if the user has at least one of the roles; otherwise, false.</returns>
    public static bool HasAnyRole(this ClaimsPrincipal principal, params string[] roles)
    {
        if (principal?.Identity?.IsAuthenticated != true || roles is null || roles.Length == 0)
            return false;

        return roles.Any(role => !string.IsNullOrWhiteSpace(role) && principal.HasRole(role));
    }

    /// <summary>
    /// Checks if the user has all of the specified roles.
    /// Case-insensitive comparison.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="roles">The role names to check.</param>
    /// <returns>True if the user has all of the roles; otherwise, false.</returns>
    public static bool HasAllRoles(this ClaimsPrincipal principal, params string[] roles)
    {
        if (principal?.Identity?.IsAuthenticated != true || roles is null || roles.Length == 0)
            return false;

        return roles.All(role => !string.IsNullOrWhiteSpace(role) && principal.HasRole(role));
    }
}

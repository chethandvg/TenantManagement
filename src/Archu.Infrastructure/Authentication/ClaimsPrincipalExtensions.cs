using System.Security.Claims;

namespace Archu.Infrastructure.Authentication;

/// <summary>
/// Extension methods for working with ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the ClaimsPrincipal.
    /// Tries multiple claim types to support different identity providers.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID if found; otherwise, null.</returns>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        // Try standard claim types
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub")
                  ?? principal.FindFirstValue("oid")
                  ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                  ?? principal.Identity.Name;

        return userId;
    }

    /// <summary>
    /// Gets the user's email from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The email if found; otherwise, null.</returns>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
    }

    /// <summary>
    /// Gets the user's name from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The name if found; otherwise, null.</returns>
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        return principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name")
            ?? principal.FindFirstValue("unique_name")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
            ?? principal.Identity.Name;
    }

    /// <summary>
    /// Gets all roles assigned to the user.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>A collection of role names.</returns>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return Enumerable.Empty<string>();

        var roleClaimTypes = new[]
        {
            ClaimTypes.Role,
            "role",
            "roles",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };

        return principal.Claims
            .Where(c => roleClaimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);
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

        return principal.IsInRole(role) || 
               principal.GetRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
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

        var userRoles = principal.GetRoles().ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roles.Any(role => !string.IsNullOrWhiteSpace(role) && userRoles.Contains(role));
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

        var userRoles = principal.GetRoles().ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roles.All(role => !string.IsNullOrWhiteSpace(role) && userRoles.Contains(role));
    }

    /// <summary>
    /// Gets a custom claim value by type.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type to search for.</param>
    /// <returns>The claim value if found; otherwise, null.</returns>
    public static string? GetClaimValue(this ClaimsPrincipal principal, string claimType)
    {
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(claimType))
            return null;

        return principal.FindFirstValue(claimType);
    }

    /// <summary>
    /// Gets all claims of a specific type.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type to search for.</param>
    /// <returns>A collection of claim values.</returns>
    public static IEnumerable<string> GetClaimValues(this ClaimsPrincipal principal, string claimType)
    {
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(claimType))
            return Enumerable.Empty<string>();

        return principal.Claims
            .Where(c => c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v));
    }
}

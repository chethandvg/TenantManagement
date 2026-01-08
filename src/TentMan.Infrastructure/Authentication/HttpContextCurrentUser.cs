using System.Security.Claims;
using TentMan.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TentMan.Infrastructure.Authentication;

/// <summary>
/// Implementation of ICurrentUser that extracts user information from the current HTTP context.
/// Supports multiple identity providers (JWT, OIDC, Azure AD, ASP.NET Core Identity).
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    // Don't rely on one claim type. Cover common IdPs (OIDC/JWT/AAD/Identity).
    private static readonly string[] UserIdClaimTypes =
    {
        ClaimTypes.NameIdentifier,                                      // ASP.NET Core Identity / classic
        "sub",                                                          // OIDC subject (standard)
        "oid",                                                          // Azure AD object id
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" // legacy WS-Federation
    };

    private static readonly string[] RoleClaimTypes =
    {
        ClaimTypes.Role,                                                // Standard .NET role claim
        "role",                                                         // OIDC role claim
        "roles",                                                        // Some OIDC providers use plural
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" // legacy WS-Federation
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpContextCurrentUser> _logger;

    public HttpContextCurrentUser(
        IHttpContextAccessor httpContextAccessor,
        ILogger<HttpContextCurrentUser> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string? UserId
    {
        get
        {
            var user = GetClaimsPrincipal();
            if (user is null || !user.Identity?.IsAuthenticated == true)
            {
                _logger.LogDebug("No authenticated user in current HTTP context");
                return null;
            }

            // Try standard claim types first
            foreach (var claimType in UserIdClaimTypes)
            {
                var value = user.FindFirst(claimType)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _logger.LogTrace("Found user ID '{UserId}' from claim type '{ClaimType}'", value, claimType);
                    return value;
                }
            }

            // Fallback: Identity.Name (not guaranteed to be a stable identifier)
            var fallbackId = user.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(fallbackId))
            {
                _logger.LogWarning(
                    "Using Identity.Name as user ID fallback. Consider adding proper NameIdentifier or 'sub' claim. Value: {UserId}",
                    fallbackId);
                return fallbackId;
            }

            _logger.LogWarning("Authenticated user found but no user ID claim present. Available claims: {Claims}",
                string.Join(", ", user.Claims.Select(c => c.Type)));

            return null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            var user = GetClaimsPrincipal();
            var isAuthenticated = user?.Identity?.IsAuthenticated == true;

            _logger.LogTrace("User authentication check: {IsAuthenticated}", isAuthenticated);

            return isAuthenticated;
        }
    }

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            _logger.LogWarning("IsInRole called with null or empty role");
            return false;
        }

        var user = GetClaimsPrincipal();
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("IsInRole check for '{Role}' failed: user not authenticated", role);
            return false;
        }

        // Check using ClaimsPrincipal.IsInRole (handles standard role claims)
        var isInRole = user.IsInRole(role);

        if (!isInRole)
        {
            // Also check custom role claim types
            isInRole = GetRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        _logger.LogTrace("User '{UserId}' role check for '{Role}': {IsInRole}",
            UserId ?? "unknown", role, isInRole);

        return isInRole;
    }

    /// <inheritdoc />
    public bool HasAnyRole(params string[] roles)
    {
        if (roles is null || roles.Length == 0)
        {
            _logger.LogWarning("HasAnyRole called with null or empty roles array");
            return false;
        }

        var user = GetClaimsPrincipal();
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("HasAnyRole check failed: user not authenticated");
            return false;
        }

        var userRoles = GetRoles().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasAnyRole = roles.Any(role => !string.IsNullOrWhiteSpace(role) && userRoles.Contains(role));

        _logger.LogTrace("User '{UserId}' has any of roles [{Roles}]: {HasAnyRole}",
            UserId ?? "unknown", string.Join(", ", roles), hasAnyRole);

        return hasAnyRole;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRoles()
    {
        var user = GetClaimsPrincipal();
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("GetRoles called for unauthenticated user");
            return Enumerable.Empty<string>();
        }

        // Extract role claims from all supported claim types
        var roles = user.Claims
            .Where(c => RoleClaimTypes.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogTrace("User '{UserId}' has roles: [{Roles}]",
            UserId ?? "unknown", string.Join(", ", roles));

        return roles;
    }

    /// <summary>
    /// Gets the ClaimsPrincipal from the current HTTP context.
    /// </summary>
    /// <returns>The ClaimsPrincipal if available; otherwise, null.</returns>
    private ClaimsPrincipal? GetClaimsPrincipal()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.User;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing HTTP context user");
            return null;
        }
    }
}

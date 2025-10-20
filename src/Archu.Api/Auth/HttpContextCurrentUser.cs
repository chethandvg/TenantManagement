using System.Security.Claims;
using Archu.Application.Abstractions;

namespace Archu.Api.Auth;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    // Don't rely on one claim type. Cover common IdPs (OIDC/JWT/AAD/Identity).
    private static readonly string[] CandidateClaimTypes =
    {
        ClaimTypes.NameIdentifier,                                      // classic
        "sub",                                                          // OIDC subject
        "oid",                                                          // Azure AD object id
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" // legacy mapping
    };

    private readonly IHttpContextAccessor _http;

    public HttpContextCurrentUser(IHttpContextAccessor http) => _http = http;

    public string? UserId
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            foreach (var type in CandidateClaimTypes)
            {
                var value = user.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            // Last-ditch: Identity.Name (not guaranteed to be a stable key)
            return user.Identity?.Name;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            var user = _http.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true;
        }
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return user.IsInRole(role);
    }

    public bool HasAnyRole(params string[] roles)
    {
        if (roles == null || roles.Length == 0)
            return false;

        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return roles.Any(role => !string.IsNullOrWhiteSpace(role) && user.IsInRole(role));
    }

    public IEnumerable<string> GetRoles()
    {
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Enumerable.Empty<string>();

        // Extract role claims - support multiple role claim types
        var roleClaims = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToList();

        return roleClaims;
    }
}

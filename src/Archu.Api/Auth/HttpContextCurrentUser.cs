using Archu.Application.Abstractions;
using System.Security.Claims;

namespace Archu.Api.Auth;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    // Don’t rely on one claim type. Cover common IdPs (OIDC/JWT/AAD/Identity).
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
            if (user?.Identity?.IsAuthenticated != true) return null;

            foreach (var type in CandidateClaimTypes)
            {
                var value = user.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }

            // Last-ditch: Identity.Name (not guaranteed to be a stable key)
            return user.Identity?.Name;
        }
    }
}
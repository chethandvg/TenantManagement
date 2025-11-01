using System.Security.Claims;

namespace Archu.ApiClient.Authentication.Models;

/// <summary>
/// Represents the current user's authentication state.
/// </summary>
public sealed record AuthenticationState
{
    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// Gets the user's claims principal.
    /// </summary>
    public ClaimsPrincipal User { get; init; } = new(new ClaimsIdentity());

    /// <summary>
    /// Gets the access token if available.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets the user's unique identifier.
    /// </summary>
    public string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value;

    /// <summary>
    /// Gets the user's name.
    /// </summary>
    public string? UserName => User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst("name")?.Value;

    /// <summary>
    /// Gets the user's email.
    /// </summary>
    public string? Email => User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("email")?.Value;

    /// <summary>
    /// Gets the user's roles.
    /// </summary>
    public IEnumerable<string> Roles => User.FindAll(ClaimTypes.Role)
        .Select(c => c.Value);

    /// <summary>
    /// Creates an authenticated state.
    /// </summary>
    public static AuthenticationState Authenticated(ClaimsPrincipal user, string accessToken) => new()
    {
        IsAuthenticated = true,
        User = user,
        AccessToken = accessToken
    };

    /// <summary>
    /// Creates an unauthenticated state.
    /// </summary>
    public static AuthenticationState Unauthenticated() => new()
    {
        IsAuthenticated = false,
        User = new ClaimsPrincipal(new ClaimsIdentity()),
        AccessToken = null
    };
}

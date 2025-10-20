using System.Security.Claims;
using Archu.ApiClient.Authentication.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Archu.ApiClient.Authentication.Providers;

/// <summary>
/// Custom authentication state provider for Blazor applications.
/// Integrates with the token manager to provide authentication state.
/// </summary>
public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenManager _tokenManager;
    private readonly ILogger<ApiAuthenticationStateProvider> _logger;

    public ApiAuthenticationStateProvider(
        ITokenManager tokenManager,
        ILogger<ApiAuthenticationStateProvider> logger)
    {
        _tokenManager = tokenManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var authState = await _tokenManager.GetAuthenticationStateAsync();

            _logger.LogDebug("Authentication state retrieved: Authenticated={IsAuthenticated}, User={User}",
                authState.IsAuthenticated,
                authState.UserName ?? "Anonymous");

            return new AuthenticationState(authState.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    /// <summary>
    /// Raises a notification that the authentication state has changed.
    /// Call this after login or logout.
    /// </summary>
    public void RaiseAuthenticationStateChanged()
    {
        _logger.LogInformation("Authentication state changed, notifying subscribers");
        // Use the base protected method to avoid ambiguity with public API
        base.NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Marks the user as authenticated with the given token.
    /// </summary>
    public void MarkUserAsAuthenticated(string accessToken)
    {
        try
        {
            var claimsPrincipal = _tokenManager.ExtractClaimsFromToken(accessToken);

            if (claimsPrincipal == null)
            {
                _logger.LogWarning("Failed to extract claims from token during authentication");
                return;
            }

            var authState = new AuthenticationState(claimsPrincipal);

            _logger.LogInformation("User marked as authenticated: {User}",
                claimsPrincipal.Identity?.Name ?? "Unknown");

            // Directly notify with the prepared state
            base.NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user as authenticated");
        }
    }

    /// <summary>
    /// Marks the user as logged out.
    /// </summary>
    public async Task MarkUserAsLoggedOutAsync()
    {
        try
        {
            await _tokenManager.RemoveTokenAsync();

            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = new AuthenticationState(anonymousUser);

            _logger.LogInformation("User marked as logged out");

            base.NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user as logged out");
        }
    }
}

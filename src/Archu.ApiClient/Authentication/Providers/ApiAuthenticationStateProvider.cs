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
    public void NotifyAuthenticationStateChanged()
    {
        _logger.LogInformation("Authentication state changed, notifying subscribers");
        // Use the base protected method to avoid ambiguity with public API
        base.NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Marks the user as authenticated.
    /// </summary>
    public async Task MarkUserAsAuthenticatedAsync(string userName)
    {
        try
        {
            _logger.LogInformation("User marked as authenticated: {UserName}", userName);

            // Notify subscribers that the authentication state has changed
            NotifyAuthenticationStateChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking user as authenticated");
        }

        await Task.CompletedTask;
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

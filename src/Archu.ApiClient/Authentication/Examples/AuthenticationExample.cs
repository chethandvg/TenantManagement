using Archu.ApiClient.Authentication.Models;
using Archu.ApiClient.Authentication.Providers;
using Archu.ApiClient.Authentication.Services;
using Microsoft.Extensions.Logging;

namespace Archu.ApiClient.Authentication.Examples;

/// <summary>
/// Example usage of the authentication framework.
/// </summary>
public class AuthenticationExample
{
    private readonly IAuthenticationService _authService;
    private readonly ITokenManager _tokenManager;
    private readonly ApiAuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthenticationExample> _logger;

    public AuthenticationExample(
        IAuthenticationService authService,
        ITokenManager tokenManager,
        ApiAuthenticationStateProvider authStateProvider,
        ILogger<AuthenticationExample> logger)
    {
        _authService = authService;
        _tokenManager = tokenManager;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Example: Login with username and password.
    /// </summary>
    public async Task<bool> LoginExampleAsync(string username, string password)
    {
        try
        {
            _logger.LogInformation("Attempting to log in user: {Username}", username);

            var result = await _authService.LoginAsync(username, password);

            if (result.Success)
            {
                _logger.LogInformation("Login successful for user: {Username}", username);
                
                // Authentication state is automatically updated
                // You can get the current state
                var authState = result.AuthenticationState;
                _logger.LogInformation("User authenticated: {UserId}, Name: {UserName}", 
                    authState?.UserId, 
                    authState?.UserName);
                
                return true;
            }
            else
            {
                _logger.LogWarning("Login failed: {Error}", result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    /// <summary>
    /// Example: Logout the current user.
    /// </summary>
    public async Task LogoutExampleAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user");
            
            await _authService.LogoutAsync();
            
            _logger.LogInformation("Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    /// <summary>
    /// Example: Check if user is authenticated.
    /// </summary>
    public async Task<bool> CheckAuthenticationStatusAsync()
    {
        try
        {
            var isAuthenticated = await _tokenManager.IsAuthenticatedAsync();
            
            if (isAuthenticated)
            {
                var authState = await _authService.GetAuthenticationStateAsync();
                _logger.LogInformation("User is authenticated: {UserId}, Name: {UserName}", 
                    authState.UserId, 
                    authState.UserName);
            }
            else
            {
                _logger.LogInformation("User is not authenticated");
            }
            
            return isAuthenticated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            return false;
        }
    }

    /// <summary>
    /// Example: Get current user information.
    /// </summary>
    public async Task<(string? UserId, string? UserName, string? Email, IEnumerable<string> Roles)> GetCurrentUserInfoAsync()
    {
        try
        {
            var authState = await _authService.GetAuthenticationStateAsync();
            
            if (!authState.IsAuthenticated)
            {
                _logger.LogInformation("No authenticated user");
                return (null, null, null, Array.Empty<string>());
            }

            return (authState.UserId, authState.UserName, authState.Email, authState.Roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user info");
            return (null, null, null, Array.Empty<string>());
        }
    }

    /// <summary>
    /// Example: Refresh authentication token.
    /// </summary>
    public async Task<bool> RefreshTokenExampleAsync()
    {
        try
        {
            _logger.LogInformation("Attempting to refresh token");
            
            var result = await _authService.RefreshTokenAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("Token refreshed successfully");
                return true;
            }
            else
            {
                _logger.LogWarning("Token refresh failed: {Error}", result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }

    /// <summary>
    /// Example: Handle authentication state changes in Blazor.
    /// </summary>
    public async Task HandleAuthenticationStateChangedAsync()
    {
        // In a Blazor component, you can subscribe to authentication state changes
        // using the AuthenticationStateProvider
        
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User is authenticated: {Name}", authState.User.Identity.Name);
            
            // Access user claims
            foreach (var claim in authState.User.Claims)
            {
                _logger.LogDebug("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }
        }
        else
        {
            _logger.LogInformation("User is not authenticated");
        }
    }
}

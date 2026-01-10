using Microsoft.AspNetCore.Components.Authorization;
using TentMan.ApiClient.Services;
using System.Security.Claims;

namespace TentMan.Ui.Services;

/// <summary>
/// Helper service for authorization checks in the UI layer.
/// Provides methods to check permissions and policies via server-side API calls.
/// </summary>
public class AuthorizationHelper
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAuthorizationApiClient _authorizationApiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationHelper"/> class.
    /// </summary>
    /// <param name="authenticationStateProvider">The authentication state provider.</param>
    /// <param name="authorizationApiClient">The authorization API client.</param>
    public AuthorizationHelper(
        AuthenticationStateProvider authenticationStateProvider,
        IAuthorizationApiClient authorizationApiClient)
    {
        _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
        _authorizationApiClient = authorizationApiClient ?? throw new ArgumentNullException(nameof(authorizationApiClient));
    }

    /// <summary>
    /// Checks if the current user has the specified permission claim via the API.
    /// </summary>
    /// <param name="permission">The permission to check (e.g., "products:read").</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    public async Task<bool> HasPermissionAsync(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be null or empty.", nameof(permission));
        }

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        try
        {
            var response = await _authorizationApiClient.CheckPermissionAsync(permission);
            return response.Success && response.Data?.IsAuthorized == true;
        }
        catch
        {
            // If API call fails, return false for safety
            return false;
        }
    }

    /// <summary>
    /// Checks if the current user satisfies the specified authorization policy via the API.
    /// </summary>
    /// <param name="policyName">The name of the policy to check.</param>
    /// <returns>True if the user satisfies the policy; otherwise, false.</returns>
    public async Task<bool> HasPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));
        }

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        try
        {
            var response = await _authorizationApiClient.CheckPolicyAsync(policyName);
            return response.Success && response.Data?.IsAuthorized == true;
        }
        catch
        {
            // If API call fails, return false for safety
            return false;
        }
    }

    /// <summary>
    /// Checks if the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user is in the role; otherwise, false.</returns>
    public async Task<bool> IsInRoleAsync(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be null or empty.", nameof(role));
        }

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.IsInRole(role);
    }

    /// <summary>
    /// Gets the current user's ClaimsPrincipal.
    /// </summary>
    /// <returns>The current user's ClaimsPrincipal.</returns>
    public async Task<ClaimsPrincipal> GetUserAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User;
    }

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    /// <returns>True if the user is authenticated; otherwise, false.</returns>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User?.Identity?.IsAuthenticated ?? false;
    }
}

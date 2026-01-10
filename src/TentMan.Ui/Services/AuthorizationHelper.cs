using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace TentMan.Ui.Services;

/// <summary>
/// Helper service for authorization checks in the UI layer.
/// Provides methods to check permissions and policies for the current user.
/// </summary>
public class AuthorizationHelper
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IAuthorizationService _authorizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationHelper"/> class.
    /// </summary>
    /// <param name="authenticationStateProvider">The authentication state provider.</param>
    /// <param name="authorizationService">The authorization service.</param>
    public AuthorizationHelper(
        AuthenticationStateProvider authenticationStateProvider,
        IAuthorizationService authorizationService)
    {
        _authenticationStateProvider = authenticationStateProvider ?? throw new ArgumentNullException(nameof(authenticationStateProvider));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    /// <summary>
    /// Checks if the current user has the specified permission claim.
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

        // Check if user has the permission claim
        return user.HasClaim(c => c.Type == "permission" && c.Value.Equals(permission, StringComparison.Ordinal));
    }

    /// <summary>
    /// Checks if the current user satisfies the specified authorization policy.
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

        // Use the authorization service to evaluate the policy
        var result = await _authorizationService.AuthorizeAsync(user, policyName);
        return result.Succeeded;
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

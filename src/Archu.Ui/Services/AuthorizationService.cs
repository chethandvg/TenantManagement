using System.Security.Claims;
using Archu.ApiClient.Authentication.Extensions;
using Microsoft.AspNetCore.Components.Authorization;

namespace Archu.Ui.Services;

/// <summary>
/// Implementation of IUiAuthorizationService that reads claims from the current authenticated user.
/// Does not hit the API - all checks are performed against local claims.
/// </summary>
public sealed class UiAuthorizationService : IUiAuthorizationService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public UiAuthorizationService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <inheritdoc/>
    public async Task<ClaimsPrincipal?> GetCurrentUserAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User;
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(string permission)
    {
        var user = await GetCurrentUserAsync();
        return user?.HasPermission(permission) ?? false;
    }

    /// <inheritdoc/>
    public async Task<bool> HasAnyPermissionAsync(params string[] permissions)
    {
        var user = await GetCurrentUserAsync();
        return user?.HasAnyPermission(permissions) ?? false;
    }

    /// <inheritdoc/>
    public async Task<bool> HasAllPermissionsAsync(params string[] permissions)
    {
        var user = await GetCurrentUserAsync();
        return user?.HasAllPermissions(permissions) ?? false;
    }

    /// <inheritdoc/>
    public async Task<bool> HasRoleAsync(string role)
    {
        var user = await GetCurrentUserAsync();
        return user?.HasRole(role) ?? false;
    }

    /// <inheritdoc/>
    public async Task<bool> HasAnyRoleAsync(params string[] roles)
    {
        var user = await GetCurrentUserAsync();
        return user?.HasAnyRole(roles) ?? false;
    }

    /// <inheritdoc/>
    public async Task<bool> HasAllRolesAsync(params string[] roles)
    {
        var user = await GetCurrentUserAsync();
        return user?.HasAllRoles(roles) ?? false;
    }
}

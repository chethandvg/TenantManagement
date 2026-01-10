using TentMan.Contracts.Authorization;
using TentMan.Contracts.Common;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Authorization API client.
/// Provides methods to check user permissions and policies server-side.
/// </summary>
public interface IAuthorizationApiClient
{
    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="permission">The permission to check (e.g., "buildings:read").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization check result.</returns>
    Task<ApiResponse<AuthorizationCheckResponse>> CheckPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user satisfies the specified policy.
    /// </summary>
    /// <param name="policyName">The policy name to check (e.g., "CanViewTenantPortal").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization check result.</returns>
    Task<ApiResponse<AuthorizationCheckResponse>> CheckPolicyAsync(
        string policyName,
        CancellationToken cancellationToken = default);
}

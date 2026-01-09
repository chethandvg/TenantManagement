using TentMan.Contracts.Authentication;
using TentMan.Contracts.Common;
using TentMan.Contracts.TenantInvites;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Tenant Invites API client.
/// </summary>
public interface ITenantInvitesApiClient
{
    /// <summary>
    /// Validates a tenant invite token.
    /// </summary>
    /// <param name="token">The invite token to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation response.</returns>
    Task<ApiResponse<ValidateInviteResponse>> ValidateInviteAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a tenant invite and creates a user account.
    /// </summary>
    /// <param name="request">The accept invite request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response with tokens.</returns>
    Task<ApiResponse<AuthenticationResponse>> AcceptInviteAsync(
        AcceptInviteRequest request,
        CancellationToken cancellationToken = default);
}

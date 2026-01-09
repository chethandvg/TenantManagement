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
    /// Validates an invite token.
    /// </summary>
    /// <param name="token">The invite token to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ApiResponse<ValidateInviteResponse>> ValidateInviteAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts an invite and creates a user account.
    /// </summary>
    /// <param name="request">The accept invite request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response with tokens.</returns>
    Task<ApiResponse<AuthenticationResponse>> AcceptInviteAsync(
        AcceptInviteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an invite for a tenant.
    /// </summary>
    /// <param name="orgId">The organization ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The generate invite request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated invite details.</returns>
    Task<ApiResponse<TenantInviteDto>> GenerateInviteAsync(
        Guid orgId,
        Guid tenantId,
        GenerateInviteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all invites for a tenant.
    /// </summary>
    /// <param name="orgId">The organization ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of invites for the tenant.</returns>
    Task<ApiResponse<IEnumerable<TenantInviteDto>>> GetInvitesByTenantAsync(
        Guid orgId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an invite.
    /// </summary>
    /// <param name="orgId">The organization ID.</param>
    /// <param name="inviteId">The invite ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the cancel operation.</returns>
    Task<ApiResponse<object>> CancelInviteAsync(
        Guid orgId,
        Guid inviteId,
        CancellationToken cancellationToken = default);
}

using TentMan.Contracts.Authentication;
using TentMan.Contracts.Common;
using TentMan.Contracts.TenantInvites;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Tenant Invites API client.
/// </summary>
public sealed class TenantInvitesApiClient : ApiClientServiceBase, ITenantInvitesApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantInvitesApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantInvitesApiClient(HttpClient httpClient, ILogger<TenantInvitesApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1";

    /// <inheritdoc/>
    public Task<ApiResponse<ValidateInviteResponse>> ValidateInviteAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return GetAsync<ValidateInviteResponse>($"invites/validate?token={Uri.EscapeDataString(token)}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<AuthenticationResponse>> AcceptInviteAsync(
        AcceptInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<AcceptInviteRequest, AuthenticationResponse>("invites/accept", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<TenantInviteDto>> GenerateInviteAsync(
        Guid orgId,
        Guid tenantId,
        GenerateInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<GenerateInviteRequest, TenantInviteDto>(
            $"organizations/{orgId}/tenants/{tenantId}/invites", 
            request, 
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<TenantInviteDto>>> GetInvitesByTenantAsync(
        Guid orgId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<TenantInviteDto>>(
            $"organizations/{orgId}/tenants/{tenantId}/invites",
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<object>> CancelInviteAsync(
        Guid orgId,
        Guid inviteId,
        CancellationToken cancellationToken = default)
    {
        var result = await DeleteAsync(
            $"organizations/{orgId}/invites/{inviteId}",
            cancellationToken);

        return new ApiResponse<object>
        {
            Success = result.Success,
            Data = null,
            Message = result.Message,
            Errors = result.Errors
        };
    }
}

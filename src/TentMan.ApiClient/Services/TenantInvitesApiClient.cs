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
    protected override string BasePath => "api/v1/invites";

    /// <inheritdoc/>
    public Task<ApiResponse<ValidateInviteResponse>> ValidateInviteAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return GetAsync<ValidateInviteResponse>($"validate?token={Uri.EscapeDataString(token)}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<AuthenticationResponse>> AcceptInviteAsync(
        AcceptInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<AcceptInviteRequest, AuthenticationResponse>("accept", request, cancellationToken);
    }
}

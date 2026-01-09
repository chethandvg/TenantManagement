using TentMan.Contracts.Common;
using TentMan.Contracts.TenantPortal;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Tenant Portal API client.
/// </summary>
public sealed class TenantPortalApiClient : ApiClientServiceBase, ITenantPortalApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantPortalApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantPortalApiClient(HttpClient httpClient, ILogger<TenantPortalApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/tenant-portal";

    /// <inheritdoc/>
    public Task<ApiResponse<TenantLeaseSummaryResponse>> GetLeaseSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<TenantLeaseSummaryResponse>("lease-summary", cancellationToken);
    }
}

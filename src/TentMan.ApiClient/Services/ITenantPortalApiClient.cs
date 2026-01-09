using TentMan.Contracts.Common;
using TentMan.Contracts.TenantPortal;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Tenant Portal API client.
/// </summary>
public interface ITenantPortalApiClient
{
    /// <summary>
    /// Gets the current tenant's active lease summary.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant's lease summary.</returns>
    Task<ApiResponse<TenantLeaseSummaryResponse>> GetLeaseSummaryAsync(
        CancellationToken cancellationToken = default);
}

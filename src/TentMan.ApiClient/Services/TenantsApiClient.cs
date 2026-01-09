using TentMan.Contracts.Common;
using TentMan.Contracts.Tenants;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Tenants API client.
/// </summary>
public sealed class TenantsApiClient : ApiClientServiceBase, ITenantsApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantsApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantsApiClient(HttpClient httpClient, ILogger<TenantsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/organizations";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<TenantListDto>>> GetTenantsAsync(
        Guid orgId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"{orgId}/tenants";
        if (!string.IsNullOrWhiteSpace(search))
        {
            endpoint += $"?search={Uri.EscapeDataString(search)}";
        }
        return GetAsync<IEnumerable<TenantListDto>>(endpoint, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<TenantDetailDto>> GetTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<TenantDetailDto>($"../../tenants/{tenantId}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<TenantListDto>> CreateTenantAsync(
        Guid orgId,
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateTenantRequest, TenantListDto>($"{orgId}/tenants", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<TenantDetailDto>> UpdateTenantAsync(
        Guid tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UpdateTenantRequest, TenantDetailDto>($"../../tenants/{tenantId}", request, cancellationToken);
    }
}

using TentMan.Contracts.Common;
using TentMan.Contracts.Tenants;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Tenants API client.
/// </summary>
public interface ITenantsApiClient
{
    /// <summary>
    /// Gets all tenants for an organization with optional search.
    /// </summary>
    /// <param name="orgId">The organization ID.</param>
    /// <param name="search">Optional search term for phone or name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of tenants.</returns>
    Task<ApiResponse<IEnumerable<TenantListDto>>> GetTenantsAsync(
        Guid orgId,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant details.</returns>
    Task<ApiResponse<TenantDetailDto>> GetTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="orgId">The organization ID.</param>
    /// <param name="request">The create tenant request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created tenant.</returns>
    Task<ApiResponse<TenantListDto>> CreateTenantAsync(
        Guid orgId,
        CreateTenantRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The update tenant request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tenant.</returns>
    Task<ApiResponse<TenantDetailDto>> UpdateTenantAsync(
        Guid tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken = default);
}

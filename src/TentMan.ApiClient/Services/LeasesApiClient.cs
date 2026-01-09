using TentMan.Contracts.Common;
using TentMan.Contracts.Leases;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Leases API client.
/// </summary>
public sealed class LeasesApiClient : ApiClientServiceBase, ILeasesApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeasesApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public LeasesApiClient(HttpClient httpClient, ILogger<LeasesApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/organizations";

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseListDto>> CreateLeaseAsync(
        Guid orgId,
        CreateLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateLeaseRequest, LeaseListDto>($"{orgId}/leases", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseDetailDto>> GetLeaseAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<LeaseDetailDto>($"../../leases/{leaseId}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<LeaseListDto>>> GetLeasesByUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<LeaseListDto>>($"../../units/{unitId}/leases", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseDetailDto>> AddLeasePartyAsync(
        Guid leaseId,
        AddLeasePartyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<AddLeasePartyRequest, LeaseDetailDto>($"../../leases/{leaseId}/parties", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseDetailDto>> AddLeaseTermAsync(
        Guid leaseId,
        AddLeaseTermRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<AddLeaseTermRequest, LeaseDetailDto>($"../../leases/{leaseId}/terms", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<LeaseDetailDto>> ActivateLeaseAsync(
        Guid leaseId,
        ActivateLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<ActivateLeaseRequest, LeaseDetailDto>($"../../leases/{leaseId}/activate", request, cancellationToken);
    }
}

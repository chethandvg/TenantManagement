using TentMan.Contracts.Common;
using TentMan.Contracts.Leases;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Leases API client.
/// </summary>
public interface ILeasesApiClient
{
    /// <summary>
    /// Creates a new lease (draft).
    /// </summary>
    /// <param name="orgId">The organization ID.</param>
    /// <param name="request">The create lease request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created lease.</returns>
    Task<ApiResponse<LeaseListDto>> CreateLeaseAsync(
        Guid orgId,
        CreateLeaseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lease by ID.
    /// </summary>
    /// <param name="leaseId">The lease ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lease details.</returns>
    Task<ApiResponse<LeaseDetailDto>> GetLeaseAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets leases for a unit.
    /// </summary>
    /// <param name="unitId">The unit ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of leases.</returns>
    Task<ApiResponse<IEnumerable<LeaseListDto>>> GetLeasesByUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a party (tenant/occupant) to a lease.
    /// </summary>
    /// <param name="leaseId">The lease ID.</param>
    /// <param name="request">The add party request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated lease.</returns>
    Task<ApiResponse<LeaseDetailDto>> AddLeasePartyAsync(
        Guid leaseId,
        AddLeasePartyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a financial term to a lease.
    /// </summary>
    /// <param name="leaseId">The lease ID.</param>
    /// <param name="request">The add term request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated lease.</returns>
    Task<ApiResponse<LeaseDetailDto>> AddLeaseTermAsync(
        Guid leaseId,
        AddLeaseTermRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a draft lease.
    /// </summary>
    /// <param name="leaseId">The lease ID.</param>
    /// <param name="request">The activate lease request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activated lease.</returns>
    Task<ApiResponse<LeaseDetailDto>> ActivateLeaseAsync(
        Guid leaseId,
        ActivateLeaseRequest request,
        CancellationToken cancellationToken = default);
}

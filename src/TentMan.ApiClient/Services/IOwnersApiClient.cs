using TentMan.Contracts.Common;
using TentMan.Contracts.Owners;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Owners API client.
/// </summary>
public interface IOwnersApiClient
{
    /// <summary>
    /// Gets all owners for an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of owners.</returns>
    Task<ApiResponse<IEnumerable<OwnerDto>>> GetOwnersAsync(
        Guid orgId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new owner.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="request">The owner creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created owner.</returns>
    Task<ApiResponse<OwnerDto>> CreateOwnerAsync(
        Guid orgId,
        CreateOwnerRequest request,
        CancellationToken cancellationToken = default);
}

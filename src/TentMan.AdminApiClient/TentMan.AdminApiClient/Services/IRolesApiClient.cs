using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the Roles Admin API client.
/// Provides methods for role management operations.
/// </summary>
public interface IRolesApiClient
{
    /// <summary>
    /// Gets all roles in the system.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of roles.</returns>
    Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="request">The create role request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created role.</returns>
    Task<ApiResponse<RoleDto>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);
}

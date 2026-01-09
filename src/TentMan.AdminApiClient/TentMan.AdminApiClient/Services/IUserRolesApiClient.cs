using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the UserRoles Admin API client.
/// Provides methods for user-role assignment management operations.
/// </summary>
public interface IUserRolesApiClient
{
    /// <summary>
    /// Gets all roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of roles assigned to the user.</returns>
    Task<ApiResponse<IEnumerable<RoleDto>>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="request">The assign role request containing userId and roleId.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the role was assigned successfully.</returns>
    Task<ApiResponse<object>> AssignRoleAsync(
        AssignRoleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleId">The role ID to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the role was removed successfully.</returns>
    Task<ApiResponse<bool>> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);
}

using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the Roles API client.
/// </summary>
/// <remarks>
/// Provides operations for role management in the Admin API.
/// Requires authentication with SuperAdmin or Administrator role.
/// </remarks>
public interface IRolesApiClient
{
    /// <summary>
    /// Retrieves all roles in the system.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all roles.</returns>
    /// <remarks>
    /// Returns all active roles including:
    /// - System roles (SuperAdmin, Administrator, Manager, User, Guest)
    /// - Custom roles created by administrators
    /// 
    /// Requires authentication with SuperAdmin, Administrator, or Manager role.
    /// </remarks>
    Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new custom role in the system.
    /// </summary>
    /// <param name="request">The role creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created role.</returns>
    /// <remarks>
    /// Validation rules:
    /// - Role name is required and must be unique (case-insensitive)
    /// - Role name length: 3-50 characters
    /// - Description is optional (max 500 characters)
    /// 
    /// Requires authentication with SuperAdmin or Administrator role.
    /// </remarks>
    Task<ApiResponse<RoleDto>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);
}

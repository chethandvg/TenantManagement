using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the UserRoles API client.
/// </summary>
/// <remarks>
/// Provides operations for user-role assignment management in the Admin API.
/// Requires authentication with appropriate permissions.
/// </remarks>
public interface IUserRolesApiClient
{
    /// <summary>
    /// Retrieves all roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">The user ID to retrieve roles for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of roles assigned to the user.</returns>
    /// <remarks>
    /// Use cases:
    /// - Check what roles a user currently has before assigning new ones
    /// - Verify user permissions for troubleshooting
    /// - Audit user access levels
    /// 
    /// Requires authentication with SuperAdmin, Administrator, or Manager role.
    /// </remarks>
    Task<ApiResponse<IEnumerable<RoleDto>>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="request">The role assignment request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Security restrictions:
    /// - SuperAdmin can assign any role (including SuperAdmin and Administrator)
    /// - Administrator can assign User, Manager, and Guest roles only
    /// - Administrator CANNOT assign SuperAdmin role
    /// - Administrator CANNOT assign Administrator role
    /// 
    /// Important notes:
    /// - Both user and role must exist in the system
    /// - If the user already has the role, an error is returned
    /// - Assignment is logged with the current admin user as the assignor
    /// 
    /// Requires authentication with SuperAdmin or Administrator role.
    /// </remarks>
    Task<ApiResponse<object>> AssignRoleAsync(
        AssignRoleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Security restrictions:
    /// - SuperAdmin can remove any role (with exceptions below)
    /// - Administrator can remove User, Manager, and Guest roles only
    /// - Administrator CANNOT remove SuperAdmin role
    /// - Administrator CANNOT remove Administrator role
    /// - Cannot remove your own SuperAdmin role
    /// - Cannot remove your own Administrator role
    /// - Cannot remove SuperAdmin role if user is the last SuperAdmin
    /// 
    /// Important notes:
    /// - Both user and role must exist in the system
    /// - If the user doesn't have the role, a 400 error is returned
    /// - Removal is logged with the current admin user
    /// - These restrictions prevent accidental privilege loss and maintain system integrity
    /// 
    /// Requires authentication with SuperAdmin or Administrator role.
    /// </remarks>
    Task<ApiResponse<object>> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);
}

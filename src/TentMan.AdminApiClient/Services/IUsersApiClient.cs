using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the Users API client.
/// </summary>
/// <remarks>
/// Provides operations for user management in the Admin API.
/// Requires authentication with SuperAdmin, Administrator, or Manager role.
/// </remarks>
public interface IUsersApiClient
{
    /// <summary>
    /// Retrieves all users in the system with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The number of users per page (default: 10, max: 100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of users.</returns>
    /// <remarks>
    /// Pagination:
    /// - Default page size: 10
    /// - Maximum page size: 100
    /// - Page numbers start at 1
    /// 
    /// Requires authentication with SuperAdmin, Administrator, or Manager role.
    /// </remarks>
    Task<ApiResponse<IEnumerable<UserDto>>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user in the system.
    /// </summary>
    /// <param name="request">The user creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created user.</returns>
    /// <remarks>
    /// Password requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// 
    /// Important notes:
    /// - Email must be unique in the system
    /// - Username must be unique in the system
    /// - User is created without roles - use UserRoles API to assign roles
    /// - Password is securely hashed using BCrypt
    /// 
    /// Requires authentication with SuperAdmin, Administrator, or Manager role.
    /// </remarks>
    Task<ApiResponse<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user from the system (soft delete).
    /// </summary>
    /// <param name="id">The user ID to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success message.</returns>
    /// <remarks>
    /// Security restrictions:
    /// - Cannot delete yourself (self-deletion protection)
    /// - Cannot delete the last SuperAdmin in the system
    /// 
    /// Notes:
    /// - This is a soft delete - user is marked as deleted but not physically removed
    /// - User's roles are preserved in case of restoration
    /// - Deleted users cannot login but data remains for audit purposes
    /// 
    /// Requires authentication with SuperAdmin or Administrator role.
    /// </remarks>
    Task<ApiResponse<bool>> DeleteUserAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

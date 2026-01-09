using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Interface for the Users Admin API client.
/// Provides methods for user management operations.
/// </summary>
public interface IUsersApiClient
{
    /// <summary>
    /// Gets all users with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The number of users per page (default: 10, max: 100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of users.</returns>
    Task<ApiResponse<IEnumerable<UserDto>>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">The create user request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created user.</returns>
    Task<ApiResponse<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user by ID.
    /// </summary>
    /// <param name="userId">The user ID to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the user was deleted successfully.</returns>
    Task<ApiResponse<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

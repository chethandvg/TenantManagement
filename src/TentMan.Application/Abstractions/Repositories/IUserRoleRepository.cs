using TentMan.Domain.Entities.Identity;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for managing user-role relationships.
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userRole">The user-role relationship to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleId">The role's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all roles from a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAllUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific role.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleId">The role's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the role; otherwise, false.</returns>
    Task<bool> UserHasRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all user-role relationships for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of user-role relationships.</returns>
    Task<IEnumerable<UserRole>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of users who have a specific role.
    /// </summary>
    /// <param name="roleId">The role's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of users with the specified role.</returns>
    Task<int> CountUsersWithRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}

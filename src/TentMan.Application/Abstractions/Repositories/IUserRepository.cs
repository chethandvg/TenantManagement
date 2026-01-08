using TentMan.Domain.Entities.Identity;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for user management operations.
/// Provides access to user data with support for querying, filtering, and relationships.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique identifier, including role relationships.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address, including role relationships.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their username, including role relationships.
    /// </summary>
    /// <param name="userName">The user's username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with pagination support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of users per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of users.</returns>
    Task<IEnumerable<ApplicationUser>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user to the database.
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added user with generated ID and RowVersion.</returns>
    Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user with optimistic concurrency control.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="originalRowVersion">The client's RowVersion for concurrency detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(
        ApplicationUser user,
        byte[] originalRowVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a user (marks as deleted).
    /// </summary>
    /// <param name="user">The user to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists by ID.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already in use.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="excludeUserId">Optional user ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email is in use; otherwise, false.</returns>
    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username is already in use.
    /// </summary>
    /// <param name="userName">The username to check.</param>
    /// <param name="excludeUserId">Optional user ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the username is in use; otherwise, false.</returns>
    Task<bool> UserNameExistsAsync(
        string userName,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of users (excluding soft-deleted).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total number of users.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

using Archu.Domain.Entities.Identity;

namespace Archu.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for role management operations.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets a role by its unique identifier, including user relationships.
    /// </summary>
    /// <param name="id">The role's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found; otherwise, null.</returns>
    Task<ApplicationRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by its name.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found; otherwise, null.</returns>
    Task<ApplicationRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all roles.</returns>
    Task<IEnumerable<ApplicationRole>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of roles assigned to the user.</returns>
    Task<IEnumerable<ApplicationRole>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role to the database.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added role with generated ID and RowVersion.</returns>
    Task<ApplicationRole> AddAsync(ApplicationRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role with optimistic concurrency control.
    /// </summary>
    /// <param name="role">The role to update.</param>
    /// <param name="originalRowVersion">The client's RowVersion for concurrency detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(
        ApplicationRole role,
        byte[] originalRowVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a role (marks as deleted).
    /// </summary>
    /// <param name="role">The role to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(ApplicationRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role exists by ID.
    /// </summary>
    /// <param name="id">The role's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role name is already in use.
    /// </summary>
    /// <param name="name">The role name to check.</param>
    /// <param name="excludeRoleId">Optional role ID to exclude from the check (for updates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role name is in use; otherwise, false.</returns>
    Task<bool> RoleNameExistsAsync(
        string name,
        Guid? excludeRoleId = null,
        CancellationToken cancellationToken = default);
}

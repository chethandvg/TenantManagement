using Archu.Domain.Entities.Identity;

namespace Archu.Domain.Abstractions.Identity;

/// <summary>
/// Defines the contract for role management operations.
/// This interface should be implemented in the Application or Infrastructure layer.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Checks if a user has a specific role.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleName">The name of the role to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the role, otherwise false.</returns>
    Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any of the specified roles.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleNames">Collection of role names to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has any of the specified roles, otherwise false.</returns>
    Task<bool> IsInAnyRoleAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has all of the specified roles.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleNames">Collection of role names to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has all of the specified roles, otherwise false.</returns>
    Task<bool> IsInAllRolesAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles assigned to a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of role names assigned to the user.</returns>
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleName">The name of the role to assign.</param>
    /// <param name="assignedBy">Who is assigning the role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role was successfully assigned, otherwise false.</returns>
    Task<bool> AssignRoleAsync(Guid userId, string roleName, string? assignedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="roleName">The name of the role to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role was successfully removed, otherwise false.</returns>
    Task<bool> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available roles in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of all available roles.</returns>
    Task<IEnumerable<ApplicationRole>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role in the system.
    /// </summary>
    /// <param name="roleName">The name of the role to create.</param>
    /// <param name="description">Optional description of the role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created role, or null if creation failed.</returns>
    Task<ApplicationRole?> CreateRoleAsync(string roleName, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role exists in the system.
    /// </summary>
    /// <param name="roleName">The name of the role to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the role exists, otherwise false.</returns>
    Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default);
}

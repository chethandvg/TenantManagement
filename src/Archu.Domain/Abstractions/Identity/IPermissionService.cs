using Archu.Domain.Enums;

namespace Archu.Domain.Abstractions.Identity;

/// <summary>
/// Defines the contract for permission-based authorization.
/// Use this for fine-grained access control beyond role-based checks.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the permission, otherwise false.</returns>
    Task<bool> HasPermissionAsync(Guid userId, Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has all of the specified permissions.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="permissions">Collection of permissions to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has all permissions, otherwise false.</returns>
    Task<bool> HasAllPermissionsAsync(Guid userId, IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any of the specified permissions.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="permissions">Collection of permissions to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has any of the permissions, otherwise false.</returns>
    Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user (aggregated from all their roles).
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined permissions from all user's roles.</returns>
    Task<Permission> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default permissions for a specific role.
    /// </summary>
    /// <param name="roleName">The name of the role.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Default permissions for the role.</returns>
    Task<Permission> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default);
}

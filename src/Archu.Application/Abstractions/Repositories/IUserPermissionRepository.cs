using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archu.Domain.Entities.Identity;

namespace Archu.Application.Abstractions.Repositories;

/// <summary>
/// Provides access to user-permission assignments enabling fine-grained authorization flows.
/// Supports querying, linking, and unlinking direct permission grants.
/// </summary>
public interface IUserPermissionRepository
{
    /// <summary>
    /// Retrieves all user-permission relationships matching the provided user identifiers.
    /// </summary>
    /// <param name="userIds">The user identifiers to scope the query.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of user-permission relationships.</returns>
    Task<IReadOnlyCollection<UserPermission>> GetByUserIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the distinct normalized permission names assigned directly to the specified user.
    /// </summary>
    /// <param name="userId">The user identifier to evaluate.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of normalized permission names.</returns>
    Task<IReadOnlyCollection<string>> GetPermissionNamesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links the provided permissions to the user while preventing duplicate assignments.
    /// </summary>
    /// <param name="userId">The user receiving the permissions.</param>
    /// <param name="permissionIds">Collection of permission identifiers to link.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    Task LinkPermissionsAsync(
        Guid userId,
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes links between the specified user and permissions when they exist.
    /// </summary>
    /// <param name="userId">The user losing the permissions.</param>
    /// <param name="permissionIds">Collection of permission identifiers to unlink.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    Task UnlinkPermissionsAsync(
        Guid userId,
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default);
}

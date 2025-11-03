using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archu.Domain.Entities.Identity;

namespace Archu.Application.Abstractions.Repositories;

/// <summary>
/// Provides access to role-permission assignments enabling default seeding and queries.
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Retrieves all role-permission relationships for the supplied role identifiers.
    /// </summary>
    /// <param name="roleIds">The role identifiers to scope the query.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of role-permission relationships.</returns>
    Task<IReadOnlyCollection<RolePermission>> GetByRoleIdsAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the distinct normalized permission names assigned to the specified roles.
    /// Intended for policy resolution scenarios where only the permission identifiers are required.
    /// </summary>
    /// <param name="roleIds">The role identifiers used to filter permissions.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of normalized permission names.</returns>
    Task<IReadOnlyCollection<string>> GetPermissionNamesByRoleIdsAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links the specified permissions to the provided role while preventing duplicate assignments.
    /// </summary>
    /// <param name="roleId">The role identifier receiving the permissions.</param>
    /// <param name="permissionIds">Collection of permission identifiers to link.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    Task LinkPermissionsAsync(
        Guid roleId,
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes links between the specified role and permissions when they exist.
    /// </summary>
    /// <param name="roleId">The role identifier losing the permissions.</param>
    /// <param name="permissionIds">Collection of permission identifiers to unlink.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    Task UnlinkPermissionsAsync(
        Guid roleId,
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a collection of role-permission relationships to the data store.
    /// </summary>
    /// <param name="rolePermissions">The role-permission relationships to add.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    Task AddRangeAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default);
}

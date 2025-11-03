using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archu.Application.Abstractions.Repositories;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Archu.Infrastructure.Repositories;

/// <summary>
/// Repository responsible for managing <see cref="RolePermission"/> relationships.
/// Supports querying and seeding default permission assignments for roles.
/// </summary>
public sealed class RolePermissionRepository : BaseRepository<RolePermission>, IRolePermissionRepository
{
    public RolePermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves all role-permission relationships matching the provided role identifiers.
    /// </summary>
    /// <param name="roleIds">Role identifiers to filter the result set.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of role-permission entities.</returns>
    public async Task<IReadOnlyCollection<RolePermission>> GetByRoleIdsAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        var roleIdList = roleIds
            .Where(roleId => roleId != Guid.Empty)
            .Distinct()
            .ToList();

        if (roleIdList.Count == 0)
        {
            return Array.Empty<RolePermission>();
        }

        return await DbSet
            .Where(rp => roleIdList.Contains(rp.RoleId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the normalized permission names assigned to any of the provided roles.
    /// </summary>
    /// <param name="roleIds">Role identifiers used to filter permission assignments.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of normalized permission names.</returns>
    public async Task<IReadOnlyCollection<string>> GetPermissionNamesByRoleIdsAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        var roleIdList = roleIds
            .Where(roleId => roleId != Guid.Empty)
            .Distinct()
            .ToList();

        if (roleIdList.Count == 0)
        {
            return Array.Empty<string>();
        }

        return await DbSet
            .AsNoTracking()
            .Where(rolePermission => roleIdList.Contains(rolePermission.RoleId))
            .Select(rolePermission => rolePermission.Permission.NormalizedName)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Links the provided permissions to the specified role while avoiding duplicate assignments.
    /// </summary>
    /// <param name="roleId">The role receiving new permission assignments.</param>
    /// <param name="permissionIds">Permission identifiers to associate with the role.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    public async Task LinkPermissionsAsync(
        Guid roleId,
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var permissionIdList = permissionIds
            .Where(permissionId => permissionId != Guid.Empty)
            .Distinct()
            .ToList();

        if (permissionIdList.Count == 0)
        {
            return;
        }

        var existingPermissionIds = await DbSet
            .Where(rolePermission => rolePermission.RoleId == roleId && permissionIdList.Contains(rolePermission.PermissionId))
            .Select(rolePermission => rolePermission.PermissionId)
            .ToListAsync(cancellationToken);

        var assignmentsToAdd = permissionIdList
            .Except(existingPermissionIds)
            .Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            })
            .ToList();

        if (assignmentsToAdd.Count == 0)
        {
            return;
        }

        await DbSet.AddRangeAsync(assignmentsToAdd, cancellationToken);
    }

    /// <summary>
    /// Removes existing permission assignments for the provided role.
    /// </summary>
    /// <param name="roleId">The role losing permission assignments.</param>
    /// <param name="permissionIds">Permission identifiers to unlink from the role.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    public async Task UnlinkPermissionsAsync(
        Guid roleId,
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var permissionIdList = permissionIds
            .Where(permissionId => permissionId != Guid.Empty)
            .Distinct()
            .ToList();

        if (permissionIdList.Count == 0)
        {
            return;
        }

        var assignmentsToRemove = await DbSet
            .Where(rolePermission => rolePermission.RoleId == roleId && permissionIdList.Contains(rolePermission.PermissionId))
            .ToListAsync(cancellationToken);

        if (assignmentsToRemove.Count == 0)
        {
            return;
        }

        DbSet.RemoveRange(assignmentsToRemove);
    }

    /// <summary>
    /// Adds the supplied role-permission entities to the database context for persistence.
    /// </summary>
    /// <param name="rolePermissions">The role-permission entities to insert.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    public async Task AddRangeAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(rolePermissions, cancellationToken);
    }
}

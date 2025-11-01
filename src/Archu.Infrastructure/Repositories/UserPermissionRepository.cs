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
/// Repository responsible for managing <see cref="UserPermission"/> relationships.
/// Handles direct permission assignments to users and supports efficient lookups.
/// </summary>
public sealed class UserPermissionRepository : BaseRepository<UserPermission>, IUserPermissionRepository
{
    public UserPermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves all user-permission relationships matching the provided user identifiers.
    /// </summary>
    /// <param name="userIds">User identifiers used to filter assignments.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of user-permission entities.</returns>
    public async Task<IReadOnlyCollection<UserPermission>> GetByUserIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var userIdList = userIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToList();

        if (userIdList.Count == 0)
        {
            return Array.Empty<UserPermission>();
        }

        return await DbSet
            .AsNoTracking()
            .Where(userPermission => userIdList.Contains(userPermission.UserId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the normalized permission names assigned directly to the specified user.
    /// </summary>
    /// <param name="userId">The user identifier used to filter permission assignments.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of normalized permission names.</returns>
    public async Task<IReadOnlyCollection<string>> GetPermissionNamesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Array.Empty<string>();
        }

        return await DbSet
            .AsNoTracking()
            .Where(userPermission => userPermission.UserId == userId)
            .Select(userPermission => userPermission.Permission.NormalizedName)
            .Distinct(StringComparer.Ordinal)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Links the provided permissions to the specified user while preventing duplicate assignments.
    /// </summary>
    /// <param name="userId">The user receiving the permissions.</param>
    /// <param name="permissionIds">Permission identifiers to associate with the user.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    public async Task LinkPermissionsAsync(
        Guid userId,
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
            .Where(userPermission => userPermission.UserId == userId && permissionIdList.Contains(userPermission.PermissionId))
            .Select(userPermission => userPermission.PermissionId)
            .ToListAsync(cancellationToken);

        var assignmentsToAdd = permissionIdList
            .Except(existingPermissionIds)
            .Select(permissionId => new UserPermission
            {
                UserId = userId,
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
    /// Removes existing permission assignments for the provided user.
    /// </summary>
    /// <param name="userId">The user losing permission assignments.</param>
    /// <param name="permissionIds">Permission identifiers to unlink from the user.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    public async Task UnlinkPermissionsAsync(
        Guid userId,
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
            .Where(userPermission => userPermission.UserId == userId && permissionIdList.Contains(userPermission.PermissionId))
            .ToListAsync(cancellationToken);

        if (assignmentsToRemove.Count == 0)
        {
            return;
        }

        DbSet.RemoveRange(assignmentsToRemove);
    }
}

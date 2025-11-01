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
        var roleIdList = roleIds.ToList();

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
    /// Adds the supplied role-permission entities to the database context for persistence.
    /// </summary>
    /// <param name="rolePermissions">The role-permission entities to insert.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    public async Task AddRangeAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(rolePermissions, cancellationToken);
    }
}

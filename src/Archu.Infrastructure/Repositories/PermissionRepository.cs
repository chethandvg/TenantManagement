using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archu.Application.Abstractions.Repositories;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Archu.Infrastructure.Repositories;

/// <summary>
/// Concrete repository for interacting with <see cref="ApplicationPermission"/> entities.
/// Centralises logic used during application initialization and administrative workflows.
/// </summary>
public sealed class PermissionRepository : BaseRepository<ApplicationPermission>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves all permissions in a read-only form for seeding and validation scenarios.
    /// </summary>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of permissions.</returns>
    public async Task<IReadOnlyCollection<ApplicationPermission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a batch of permissions to the database context for persistence.
    /// </summary>
    /// <param name="permissions">The permissions to insert.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    public async Task AddRangeAsync(IEnumerable<ApplicationPermission> permissions, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(permissions, cancellationToken);
    }
}

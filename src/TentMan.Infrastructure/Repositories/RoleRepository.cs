using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities.Identity;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for role management operations.
/// </summary>
public sealed class RoleRepository : BaseRepository<ApplicationRole>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<ApplicationRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApplicationRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToUpperInvariant();

        return await DbSet
            .Include(r => r.UserRoles)
            .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationRole>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationRole>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.UserRoles.Any(ur => ur.UserId == userId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApplicationRole> AddAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        DbSet.Add(role);
        return Task.FromResult(role);
    }

    /// <inheritdoc />
    public Task UpdateAsync(
        ApplicationRole role,
        byte[] originalRowVersion,
        CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(role, originalRowVersion);
        DbSet.Update(role);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        SoftDelete(role);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().AnyAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RoleNameExistsAsync(
        string name,
        Guid? excludeRoleId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToUpperInvariant();

        var query = DbSet.AsNoTracking().Where(r => r.NormalizedName == normalizedName);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

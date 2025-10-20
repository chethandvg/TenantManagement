using Archu.Application.Abstractions.Repositories;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Archu.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user management operations.
/// </summary>
public sealed class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();

        return await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationUser>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        DbSet.Add(user);
        return Task.FromResult(user);
    }

    /// <inheritdoc />
    public Task UpdateAsync(
        ApplicationUser user,
        byte[] originalRowVersion,
        CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(user, originalRowVersion);
        DbSet.Update(user);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        SoftDelete(user);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().AnyAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();

        var query = DbSet.AsNoTracking().Where(u => u.NormalizedEmail == normalizedEmail);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UserNameExistsAsync(
        string userName,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(u => u.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().CountAsync(cancellationToken);
    }
}

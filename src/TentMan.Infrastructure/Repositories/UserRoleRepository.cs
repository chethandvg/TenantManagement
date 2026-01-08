using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities.Identity;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing user-role relationships.
/// </summary>
public sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly ApplicationDbContext _context;

    public UserRoleRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        _context.UserRoles.Add(userRole);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole is not null)
        {
            _context.UserRoles.Remove(userRole);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAllUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.UserRoles.RemoveRange(userRoles);
    }

    /// <inheritdoc />
    public async Task<bool> UserHasRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserRole>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountUsersWithRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .CountAsync(ur => ur.RoleId == roleId, cancellationToken);
    }
}

using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class TenantInviteRepository : BaseRepository<TenantInvite>, ITenantInviteRepository
{
    public TenantInviteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TenantInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<TenantInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.InviteToken == token && !i.IsDeleted, cancellationToken);
    }

    public async Task<TenantInvite?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Tenant)
            .Where(i => i.TenantId == tenantId && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<TenantInvite>> GetAllByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Tenant)
            .Where(i => i.TenantId == tenantId && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantInvite> AddAsync(TenantInvite invite, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(invite, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(TenantInvite invite, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        Context.Entry(invite).OriginalValues[nameof(TenantInvite.RowVersion)] = originalRowVersion;
        DbSet.Update(invite);
        return Task.CompletedTask;
    }

    public async Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(i => i.InviteToken == token && !i.IsDeleted, cancellationToken);
    }
}

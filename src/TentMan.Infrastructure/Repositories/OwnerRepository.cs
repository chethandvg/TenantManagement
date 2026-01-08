using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class OwnerRepository : BaseRepository<Owner>, IOwnerRepository
{
    public OwnerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Owner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Owner>> GetByOrganizationIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.OrgId == orgId)
            .OrderBy(o => o.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Owner> AddAsync(Owner owner, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(owner, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Owner owner, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(owner, originalRowVersion);
        DbSet.Update(owner);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
    }
}

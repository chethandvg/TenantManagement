using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class OrganizationRepository : BaseRepository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Organization> AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(organization, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Organization organization, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(organization, originalRowVersion);
        DbSet.Update(organization);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
    }
}

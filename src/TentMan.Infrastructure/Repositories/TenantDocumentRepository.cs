using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class TenantDocumentRepository : BaseRepository<TenantDocument>, ITenantDocumentRepository
{
    public TenantDocumentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TenantDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.File)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TenantDocument>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.File)
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantDocument> AddAsync(TenantDocument document, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(document, cancellationToken);
        return entry.Entity;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(d => d.Id == id, cancellationToken);
    }
}

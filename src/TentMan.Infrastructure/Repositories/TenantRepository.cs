using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.Addresses.Where(a => !a.IsDeleted))
            .Include(t => t.EmergencyContacts.Where(c => !c.IsDeleted))
            .Include(t => t.Documents.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.File)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Tenant>> GetByOrgIdAsync(Guid orgId, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(t => t.OrgId == orgId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(t => 
                t.FullName.ToLower().Contains(search) ||
                t.Phone.Contains(search) ||
                (t.Email != null && t.Email.ToLower().Contains(search)));
        }

        return await query
            .OrderBy(t => t.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(tenant, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Tenant tenant, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(tenant, originalRowVersion);
        DbSet.Update(tenant);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<bool> PhoneExistsAsync(Guid orgId, string phone, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(t => t.OrgId == orgId && t.Phone == phone && !t.IsDeleted);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(Guid orgId, string email, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(t => t.OrgId == orgId && t.Email == email && !t.IsDeleted);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

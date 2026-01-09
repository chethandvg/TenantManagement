using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class LeaseRepository : BaseRepository<Lease>, ILeaseRepository
{
    public LeaseRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Lease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Lease?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(l => l.Unit)
                .ThenInclude(u => u.Building)
            .Include(l => l.Parties.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Tenant)
            .Include(l => l.Terms.Where(t => !t.IsDeleted))
            .Include(l => l.DepositTransactions.Where(d => !d.IsDeleted))
            .Include(l => l.Handovers.Where(h => !h.IsDeleted))
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Lease>> GetByUnitIdAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.UnitId == unitId)
            .Include(l => l.Parties.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Tenant)
            .Include(l => l.Terms.Where(t => !t.IsDeleted))
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Lease>> GetByOrgIdAsync(Guid orgId, LeaseStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(l => l.OrgId == orgId);

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        return await query
            .Include(l => l.Unit)
            .Include(l => l.Parties.Where(p => !p.IsDeleted && p.Role == LeasePartyRole.PrimaryTenant))
                .ThenInclude(p => p.Tenant)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lease> AddAsync(Lease lease, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(lease, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Lease lease, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(lease, originalRowVersion);
        DbSet.Update(lease);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
    }

    public async Task<bool> HasActiveLeaseAsync(Guid unitId, Guid? excludeLeaseId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(l => l.UnitId == unitId && 
            (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.NoticeGiven));

        if (excludeLeaseId.HasValue)
        {
            query = query.Where(l => l.Id != excludeLeaseId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Lease?> GetActiveLeaseForUnitAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.UnitId == unitId && 
                (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.NoticeGiven))
            .Include(l => l.Parties.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Tenant)
            .Include(l => l.Terms.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

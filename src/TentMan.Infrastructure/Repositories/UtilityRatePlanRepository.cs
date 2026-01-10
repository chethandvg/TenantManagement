using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class UtilityRatePlanRepository : BaseRepository<UtilityRatePlan>, IUtilityRatePlanRepository
{
    public UtilityRatePlanRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UtilityRatePlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<UtilityRatePlan?> GetByIdWithSlabsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.RateSlabs.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<UtilityRatePlan>> GetByOrgIdAsync(Guid orgId, UtilityType? utilityType = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => p.OrgId == orgId);

        if (utilityType.HasValue)
        {
            query = query.Where(p => p.UtilityType == utilityType.Value);
        }

        return await query
            .OrderBy(p => p.UtilityType)
            .ThenByDescending(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<UtilityRatePlan?> GetActiveRatePlanAsync(Guid orgId, UtilityType utilityType, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.RateSlabs.Where(s => !s.IsDeleted))
            .Where(p => p.OrgId == orgId 
                && p.UtilityType == utilityType 
                && p.IsActive
                && p.EffectiveFrom <= effectiveDate
                && (p.EffectiveTo == null || p.EffectiveTo >= effectiveDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UtilityRatePlan> AddAsync(UtilityRatePlan ratePlan, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(ratePlan, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(UtilityRatePlan ratePlan, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(ratePlan, originalRowVersion);
        DbSet.Update(ratePlan);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
}

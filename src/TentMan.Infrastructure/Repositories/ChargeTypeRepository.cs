using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class ChargeTypeRepository : BaseRepository<ChargeType>, IChargeTypeRepository
{
    public ChargeTypeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ChargeType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ChargeType?> GetByCodeAsync(ChargeTypeCode code, Guid? orgId = null, CancellationToken cancellationToken = default)
    {
        // First try to get organization-specific charge type if orgId is provided
        if (orgId.HasValue)
        {
            var orgChargeType = await DbSet
                .FirstOrDefaultAsync(c => c.Code == code && c.OrgId == orgId, cancellationToken);
            
            if (orgChargeType != null)
                return orgChargeType;
        }

        // Fall back to system-defined charge type
        return await DbSet
            .FirstOrDefaultAsync(c => c.Code == code && c.OrgId == null && c.IsSystemDefined, cancellationToken);
    }

    public async Task<IEnumerable<ChargeType>> GetByOrgIdAsync(Guid? orgId = null, bool? isActive = true, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        // Get both system-defined and org-specific charge types
        if (orgId.HasValue)
        {
            query = query.Where(c => c.OrgId == null || c.OrgId == orgId);
        }
        else
        {
            query = query.Where(c => c.OrgId == null);
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(c => c.Code)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChargeType> AddAsync(ChargeType chargeType, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(chargeType, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(ChargeType chargeType, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(chargeType, originalRowVersion);
        DbSet.Update(chargeType);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }
}

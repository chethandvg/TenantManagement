using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class UtilityStatementRepository : BaseRepository<UtilityStatement>, IUtilityStatementRepository
{
    public UtilityStatementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UtilityStatement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<UtilityStatement>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(u => u.LeaseId == leaseId)
            .OrderByDescending(u => u.BillingPeriodEnd)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UtilityStatement>> GetByUnitIdAsync(Guid unitId, UtilityType? utilityType = null, CancellationToken cancellationToken = default)
    {
        // First get all leases for the unit
        var leaseIds = await Context.Leases
            .Where(l => l.UnitId == unitId && !l.IsDeleted)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var query = DbSet.Where(u => leaseIds.Contains(u.LeaseId));

        if (utilityType.HasValue)
        {
            query = query.Where(u => u.UtilityType == utilityType.Value);
        }

        return await query
            .OrderByDescending(u => u.BillingPeriodEnd)
            .ToListAsync(cancellationToken);
    }

    public async Task<UtilityStatement> AddAsync(UtilityStatement statement, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(statement, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return statement;
    }

    public async Task UpdateAsync(UtilityStatement statement, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        // EF Core will handle optimistic concurrency check through RowVersion
        Context.Entry(statement).Property(nameof(statement.RowVersion)).OriginalValue = originalRowVersion;
        DbSet.Update(statement);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(u => u.Id == id, cancellationToken);
    }
}

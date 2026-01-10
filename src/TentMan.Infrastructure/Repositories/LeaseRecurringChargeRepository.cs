using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class LeaseRecurringChargeRepository : BaseRepository<LeaseRecurringCharge>, ILeaseRecurringChargeRepository
{
    public LeaseRecurringChargeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<LeaseRecurringCharge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.ChargeType)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<LeaseRecurringCharge>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.ChargeType)
            .Where(c => c.LeaseId == leaseId)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LeaseRecurringCharge>> GetActiveByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.ChargeType)
            .Where(c => c.LeaseId == leaseId && c.IsActive)
            .OrderBy(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaseRecurringCharge> AddAsync(LeaseRecurringCharge charge, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(charge, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(LeaseRecurringCharge charge, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(charge, originalRowVersion);
        DbSet.Update(charge);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }
}

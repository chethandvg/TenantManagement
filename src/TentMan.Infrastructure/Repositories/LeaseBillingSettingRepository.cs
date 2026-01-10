using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class LeaseBillingSettingRepository : BaseRepository<LeaseBillingSetting>, ILeaseBillingSettingRepository
{
    public LeaseBillingSettingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<LeaseBillingSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<LeaseBillingSetting?> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.LeaseId == leaseId, cancellationToken);
    }

    public async Task<LeaseBillingSetting> AddAsync(LeaseBillingSetting setting, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(setting, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(LeaseBillingSetting setting, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(setting, originalRowVersion);
        DbSet.Update(setting);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
    }
}

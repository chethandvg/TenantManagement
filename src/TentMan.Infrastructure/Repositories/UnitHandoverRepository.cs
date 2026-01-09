using Microsoft.EntityFrameworkCore;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;

namespace TentMan.Infrastructure.Repositories;

public class UnitHandoverRepository : BaseRepository<UnitHandover>, IUnitHandoverRepository
{
    public UnitHandoverRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UnitHandover?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, cancellationToken);
    }

    public async Task<UnitHandover?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.ChecklistItems.Where(ci => !ci.IsDeleted))
                .ThenInclude(ci => ci.PhotoFile)
            .Include(h => h.Lease)
                .ThenInclude(l => l.Unit)
                    .ThenInclude(u => u.Building)
            .Include(h => h.Lease)
                .ThenInclude(l => l.MeterReadings.Where(mr => !mr.IsDeleted))
            .Include(h => h.SignatureTenantFile)
            .Include(h => h.SignatureOwnerFile)
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, cancellationToken);
    }

    public async Task<UnitHandover?> GetByLeaseIdAsync(Guid leaseId, HandoverType type, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.ChecklistItems.Where(ci => !ci.IsDeleted))
                .ThenInclude(ci => ci.PhotoFile)
            .Include(h => h.Lease)
                .ThenInclude(l => l.Unit)
                    .ThenInclude(u => u.Building)
            .Include(h => h.Lease)
                .ThenInclude(l => l.MeterReadings.Where(mr => !mr.IsDeleted))
            .Include(h => h.SignatureTenantFile)
            .Include(h => h.SignatureOwnerFile)
            .FirstOrDefaultAsync(h => h.LeaseId == leaseId && h.Type == type && !h.IsDeleted, cancellationToken);
    }

    public async Task<UnitHandover> AddAsync(UnitHandover handover, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(handover, cancellationToken);
        return handover;
    }

    public async Task UpdateAsync(UnitHandover handover, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        Context.Entry(handover).Property(h => h.RowVersion).OriginalValue = originalRowVersion;
        DbSet.Update(handover);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(h => h.Id == id && !h.IsDeleted, cancellationToken);
    }
}

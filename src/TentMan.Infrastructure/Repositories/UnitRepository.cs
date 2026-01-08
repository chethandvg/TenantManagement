using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class UnitRepository : BaseRepository<Unit>, IUnitRepository
{
    public UnitRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Unit?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Building)
                .ThenInclude(b => b.OwnershipShares.Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow))
                    .ThenInclude(s => s.Owner)
            .Include(u => u.OwnershipShares.Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow))
                .ThenInclude(s => s.Owner)
            .Include(u => u.Meters.Where(m => m.IsActive))
            .Include(u => u.UnitFiles)
                .ThenInclude(uf => uf.File)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Unit>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(u => u.BuildingId == buildingId)
            .OrderBy(u => u.Floor)
                .ThenBy(u => u.UnitNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Unit> AddAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(unit, cancellationToken);
        return entry.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<Unit> units, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(units, cancellationToken);
    }

    public Task UpdateAsync(Unit unit, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(unit, originalRowVersion);
        DbSet.Update(unit);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> UnitNumberExistsAsync(Guid buildingId, string unitNumber, Guid? excludeUnitId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.BuildingId == buildingId && u.UnitNumber == unitNumber);
        
        if (excludeUnitId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUnitId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

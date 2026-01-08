using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class UnitRepository : IUnitRepository
{
    private readonly ApplicationDbContext _context;

    public UnitRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Units
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Unit?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Units
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
        return await _context.Units
            .Where(u => u.BuildingId == buildingId)
            .OrderBy(u => u.Floor)
                .ThenBy(u => u.UnitNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Unit> AddAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Units.AddAsync(unit, cancellationToken);
        return entry.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<Unit> units, CancellationToken cancellationToken = default)
    {
        await _context.Units.AddRangeAsync(units, cancellationToken);
    }

    public Task UpdateAsync(Unit unit, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        _context.Entry(unit).OriginalValues[nameof(unit.RowVersion)] = originalRowVersion;
        _context.Units.Update(unit);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Units.AnyAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> UnitNumberExistsAsync(Guid buildingId, string unitNumber, Guid? excludeUnitId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Units.Where(u => u.BuildingId == buildingId && u.UnitNumber == unitNumber);
        
        if (excludeUnitId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUnitId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

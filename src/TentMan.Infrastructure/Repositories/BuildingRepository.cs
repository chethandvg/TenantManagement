using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class BuildingRepository : IBuildingRepository
{
    private readonly ApplicationDbContext _context;

    public BuildingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Building?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Building?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Buildings
            .Include(b => b.Address)
            .Include(b => b.OwnershipShares.Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow))
                .ThenInclude(s => s.Owner)
            .Include(b => b.BuildingFiles)
                .ThenInclude(bf => bf.File)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Building>> GetByOrganizationIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await _context.Buildings
            .Include(b => b.Address)
            .Include(b => b.Units)
            .Where(b => b.OrgId == orgId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Building> AddAsync(Building building, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Buildings.AddAsync(building, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Building building, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        _context.Entry(building).OriginalValues[nameof(building.RowVersion)] = originalRowVersion;
        _context.Buildings.Update(building);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Buildings.AnyAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<bool> BuildingCodeExistsAsync(Guid orgId, string buildingCode, Guid? excludeBuildingId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Buildings.Where(b => b.OrgId == orgId && b.BuildingCode == buildingCode);
        
        if (excludeBuildingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBuildingId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

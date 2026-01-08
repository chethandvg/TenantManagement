using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class BuildingRepository : BaseRepository<Building>, IBuildingRepository
{
    public BuildingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Building?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Building?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.Address)
            .Include(b => b.OwnershipShares.Where(s => s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow))
                .ThenInclude(s => s.Owner)
            .Include(b => b.BuildingFiles)
                .ThenInclude(bf => bf.File)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Building>> GetByOrganizationIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.Address)
            .Include(b => b.Units)
            .Where(b => b.OrgId == orgId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Building> AddAsync(Building building, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(building, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Building building, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(building, originalRowVersion);
        DbSet.Update(building);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);
    }

    public async Task<bool> BuildingCodeExistsAsync(Guid orgId, string buildingCode, Guid? excludeBuildingId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(b => b.OrgId == orgId && b.BuildingCode == buildingCode);
        
        if (excludeBuildingId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBuildingId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

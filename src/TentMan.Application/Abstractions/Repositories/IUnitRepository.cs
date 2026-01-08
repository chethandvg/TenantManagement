using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

public interface IUnitRepository
{
    Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Unit?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Unit>> GetByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<Unit> AddAsync(Unit unit, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Unit> units, CancellationToken cancellationToken = default);
    Task UpdateAsync(Unit unit, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UnitNumberExistsAsync(Guid buildingId, string unitNumber, Guid? excludeUnitId = null, CancellationToken cancellationToken = default);
}

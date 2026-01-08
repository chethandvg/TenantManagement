using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

public interface IBuildingRepository
{
    Task<Building?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Building?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Building>> GetByOrganizationIdAsync(Guid orgId, CancellationToken cancellationToken = default);
    Task<Building> AddAsync(Building building, CancellationToken cancellationToken = default);
    Task UpdateAsync(Building building, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> BuildingCodeExistsAsync(Guid orgId, string buildingCode, Guid? excludeBuildingId = null, CancellationToken cancellationToken = default);
}

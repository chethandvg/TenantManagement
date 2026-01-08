using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for Lease entity operations.
/// </summary>
public interface ILeaseRepository
{
    Task<Lease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lease?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lease>> GetByUnitIdAsync(Guid unitId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lease>> GetByOrgIdAsync(Guid orgId, LeaseStatus? status = null, CancellationToken cancellationToken = default);
    Task<Lease> AddAsync(Lease lease, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lease lease, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveLeaseAsync(Guid unitId, Guid? excludeLeaseId = null, CancellationToken cancellationToken = default);
    Task<Lease?> GetActiveLeaseForUnitAsync(Guid unitId, CancellationToken cancellationToken = default);
}

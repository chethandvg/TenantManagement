using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for UnitHandover entity operations.
/// </summary>
public interface IUnitHandoverRepository
{
    Task<UnitHandover?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitHandover?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitHandover?> GetByLeaseIdAsync(Guid leaseId, HandoverType type, CancellationToken cancellationToken = default);
    Task<UnitHandover> AddAsync(UnitHandover handover, CancellationToken cancellationToken = default);
    Task UpdateAsync(UnitHandover handover, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

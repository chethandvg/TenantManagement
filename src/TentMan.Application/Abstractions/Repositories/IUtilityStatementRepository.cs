using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for UtilityStatement entity operations.
/// </summary>
public interface IUtilityStatementRepository
{
    Task<UtilityStatement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UtilityStatement>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UtilityStatement>> GetByUnitIdAsync(Guid unitId, UtilityType? utilityType = null, CancellationToken cancellationToken = default);
    Task<UtilityStatement> AddAsync(UtilityStatement statement, CancellationToken cancellationToken = default);
    Task UpdateAsync(UtilityStatement statement, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

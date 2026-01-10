using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for UtilityRatePlan entity operations.
/// </summary>
public interface IUtilityRatePlanRepository
{
    Task<UtilityRatePlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UtilityRatePlan?> GetByIdWithSlabsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UtilityRatePlan>> GetByOrgIdAsync(Guid orgId, UtilityType? utilityType = null, CancellationToken cancellationToken = default);
    Task<UtilityRatePlan?> GetActiveRatePlanAsync(Guid orgId, UtilityType utilityType, DateOnly effectiveDate, CancellationToken cancellationToken = default);
    Task<UtilityRatePlan> AddAsync(UtilityRatePlan ratePlan, CancellationToken cancellationToken = default);
    Task UpdateAsync(UtilityRatePlan ratePlan, byte[] originalRowVersion, CancellationToken cancellationToken = default);
}

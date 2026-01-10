using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for ChargeType entity operations.
/// </summary>
public interface IChargeTypeRepository
{
    Task<ChargeType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChargeType>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<ChargeType?> GetByCodeAsync(ChargeTypeCode code, Guid? orgId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChargeType>> GetByOrgIdAsync(Guid? orgId = null, bool? isActive = true, CancellationToken cancellationToken = default);
    Task<ChargeType> AddAsync(ChargeType chargeType, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChargeType chargeType, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

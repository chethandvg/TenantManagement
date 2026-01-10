using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for LeaseBillingSetting entity operations.
/// </summary>
public interface ILeaseBillingSettingRepository
{
    Task<LeaseBillingSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaseBillingSetting?> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);
    Task<LeaseBillingSetting> AddAsync(LeaseBillingSetting setting, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaseBillingSetting setting, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

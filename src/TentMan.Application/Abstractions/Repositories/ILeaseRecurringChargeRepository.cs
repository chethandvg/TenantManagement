using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for LeaseRecurringCharge entity operations.
/// </summary>
public interface ILeaseRecurringChargeRepository
{
    Task<LeaseRecurringCharge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaseRecurringCharge>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaseRecurringCharge>> GetActiveByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);
    Task<LeaseRecurringCharge> AddAsync(LeaseRecurringCharge charge, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaseRecurringCharge charge, byte[] originalRowVersion, CancellationToken cancellationToken = default);
}

using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for Invoice entity operations.
/// </summary>
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetByOrgIdAsync(Guid orgId, InvoiceStatus? status = null, CancellationToken cancellationToken = default);
    Task<Invoice?> GetDraftInvoiceForPeriodAsync(Guid leaseId, DateOnly billingPeriodStart, DateOnly billingPeriodEnd, CancellationToken cancellationToken = default);
    Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for InvoiceRun entity operations.
/// </summary>
public interface IInvoiceRunRepository
{
    Task<InvoiceRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InvoiceRun?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvoiceRun>> GetByOrgIdAsync(Guid orgId, InvoiceRunStatus? status = null, CancellationToken cancellationToken = default);
    Task<InvoiceRun> AddAsync(InvoiceRun invoiceRun, CancellationToken cancellationToken = default);
    Task UpdateAsync(InvoiceRun invoiceRun, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

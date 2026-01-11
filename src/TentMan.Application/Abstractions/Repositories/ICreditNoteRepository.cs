using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for CreditNote entity operations.
/// </summary>
public interface ICreditNoteRepository
{
    Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreditNote?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditNote>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CreditNote>> GetByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default);
    Task<CreditNote> AddAsync(CreditNote creditNote, CancellationToken cancellationToken = default);
    Task UpdateAsync(CreditNote creditNote, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

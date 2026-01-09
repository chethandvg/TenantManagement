using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for TenantDocument entity operations.
/// </summary>
public interface ITenantDocumentRepository
{
    Task<TenantDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantDocument>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantDocument> AddAsync(TenantDocument document, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

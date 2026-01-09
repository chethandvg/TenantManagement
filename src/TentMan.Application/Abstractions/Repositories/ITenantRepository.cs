using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for Tenant entity operations.
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> GetByOrgIdAsync(Guid orgId, string? search = null, CancellationToken cancellationToken = default);
    Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant tenant, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PhoneExistsAsync(Guid orgId, string phone, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Guid orgId, string email, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByLinkedUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

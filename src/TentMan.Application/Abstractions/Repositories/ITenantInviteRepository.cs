using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for TenantInvite entity operations.
/// </summary>
public interface ITenantInviteRepository
{
    Task<TenantInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<TenantInvite?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantInvite> AddAsync(TenantInvite invite, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantInvite invite, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken = default);
}

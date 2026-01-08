using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

public interface IOwnerRepository
{
    Task<Owner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Owner>> GetByOrganizationIdAsync(Guid orgId, CancellationToken cancellationToken = default);
    Task<Owner> AddAsync(Owner owner, CancellationToken cancellationToken = default);
    Task UpdateAsync(Owner owner, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

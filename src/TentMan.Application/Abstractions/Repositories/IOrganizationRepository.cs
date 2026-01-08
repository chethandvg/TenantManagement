using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Organization> AddAsync(Organization organization, CancellationToken cancellationToken = default);
    Task UpdateAsync(Organization organization, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

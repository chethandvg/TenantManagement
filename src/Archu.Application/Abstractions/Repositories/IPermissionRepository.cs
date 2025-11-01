using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archu.Domain.Entities.Identity;

namespace Archu.Application.Abstractions.Repositories;

/// <summary>
/// Provides data access abstractions for managing application permissions.
/// Enables seeding and querying discrete permission definitions.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Retrieves all permissions defined within the system.
    /// </summary>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    /// <returns>A read-only collection of permissions.</returns>
    Task<IReadOnlyCollection<ApplicationPermission>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a batch of permissions to the underlying data store.
    /// </summary>
    /// <param name="permissions">The permission entities to add.</param>
    /// <param name="cancellationToken">Token that propagates notification that operations should be cancelled.</param>
    Task AddRangeAsync(IEnumerable<ApplicationPermission> permissions, CancellationToken cancellationToken = default);
}

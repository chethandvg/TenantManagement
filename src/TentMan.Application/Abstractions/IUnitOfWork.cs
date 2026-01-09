using TentMan.Application.Abstractions.Repositories;

namespace TentMan.Application.Abstractions;

/// <summary>
/// Defines a unit of work for managing transactions and repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the product repository.
    /// </summary>
    IProductRepository Products { get; }

    /// <summary>
    /// Gets the organization repository.
    /// </summary>
    IOrganizationRepository Organizations { get; }

    /// <summary>
    /// Gets the building repository.
    /// </summary>
    IBuildingRepository Buildings { get; }

    /// <summary>
    /// Gets the unit repository.
    /// </summary>
    IUnitRepository Units { get; }

    /// <summary>
    /// Gets the owner repository.
    /// </summary>
    IOwnerRepository Owners { get; }

    /// <summary>
    /// Gets the user repository.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the role repository.
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Gets the user-role repository.
    /// </summary>
    IUserRoleRepository UserRoles { get; }

    /// <summary>
    /// Gets the email confirmation token repository.
    /// </summary>
    IEmailConfirmationTokenRepository EmailConfirmationTokens { get; }

    /// <summary>
    /// Gets the password reset token repository.
    /// </summary>
    IPasswordResetTokenRepository PasswordResetTokens { get; }

    /// <summary>
    /// Gets the tenant repository.
    /// </summary>
    ITenantRepository Tenants { get; }

    /// <summary>
    /// Gets the lease repository.
    /// </summary>
    ILeaseRepository Leases { get; }

    /// <summary>
    /// Gets the file metadata repository.
    /// </summary>
    IFileMetadataRepository FileMetadata { get; }

    /// <summary>
    /// Executes an operation with retry logic for transient failures.
    /// Required when using transactions with database retry-on-failure strategies.
    /// </summary>
    /// <typeparam name="TResult">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TResult> ExecuteWithRetryAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

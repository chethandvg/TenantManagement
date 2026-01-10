namespace TentMan.Application.Abstractions;

/// <summary>
/// Abstraction for database context to maintain clean architecture separation.
/// Allows the Application layer to trigger persistence without depending on Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

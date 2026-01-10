namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository for managing number sequences (invoice, credit note, etc.).
/// Thread-safe operations for generating sequential numbers.
/// </summary>
public interface INumberSequenceRepository
{
    /// <summary>
    /// Gets the next sequence number for a specific type and organization.
    /// This method must be thread-safe and guarantee uniqueness.
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="sequenceType">Type of sequence (e.g., "Invoice", "CreditNote")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next sequence number</returns>
    Task<long> GetNextSequenceNumberAsync(
        Guid orgId,
        string sequenceType,
        CancellationToken cancellationToken = default);
}

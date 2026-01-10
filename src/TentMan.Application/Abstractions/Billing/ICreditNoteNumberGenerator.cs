namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Interface for generating unique credit note numbers.
/// Implementations must be thread-safe.
/// </summary>
public interface ICreditNoteNumberGenerator
{
    /// <summary>
    /// Generates the next credit note number for an organization.
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="prefix">Optional prefix for the credit note number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unique credit note number</returns>
    Task<string> GenerateNextAsync(
        Guid orgId,
        string? prefix = null,
        CancellationToken cancellationToken = default);
}

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Interface for generating unique invoice numbers.
/// Implementations must be thread-safe.
/// </summary>
public interface IInvoiceNumberGenerator
{
    /// <summary>
    /// Generates the next invoice number for an organization.
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="prefix">Optional prefix for the invoice number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unique invoice number</returns>
    Task<string> GenerateNextAsync(
        Guid orgId,
        string? prefix = null,
        CancellationToken cancellationToken = default);
}

using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Service for generating invoices for leases.
/// Handles rent, recurring charges, and utility billing.
/// </summary>
public interface IInvoiceGenerationService
{
    /// <summary>
    /// Generates an invoice for a lease and billing period.
    /// Idempotent - will update existing draft invoice if found.
    /// </summary>
    /// <param name="leaseId">The lease ID</param>
    /// <param name="billingPeriodStart">Start of the billing period</param>
    /// <param name="billingPeriodEnd">End of the billing period (inclusive)</param>
    /// <param name="prorationMethod">Method to use for proration calculations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated or updated invoice</returns>
    Task<InvoiceGenerationResult> GenerateInvoiceAsync(
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of invoice generation.
/// </summary>
public class InvoiceGenerationResult
{
    public bool IsSuccess { get; init; }
    public Invoice? Invoice { get; init; }
    public bool WasUpdated { get; init; }
    public string? ErrorMessage { get; init; }
}

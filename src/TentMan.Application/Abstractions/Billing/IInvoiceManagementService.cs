using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Service for managing invoice lifecycle operations (issue, void).
/// </summary>
public interface IInvoiceManagementService
{
    /// <summary>
    /// Issues an invoice, transitioning it from Draft to Issued status.
    /// Sets the IssueDate and marks the invoice as immutable.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice to issue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the issued invoice or error message.</returns>
    Task<InvoiceIssueResult> IssueInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids an invoice with a reason.
    /// Voided invoices cannot be edited or un-voided.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice to void.</param>
    /// <param name="reason">The reason for voiding the invoice.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the voided invoice or error message.</returns>
    Task<InvoiceVoidResult> VoidInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of invoice issue operation.
/// </summary>
public class InvoiceIssueResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Invoice? Invoice { get; set; }
}

/// <summary>
/// Result of invoice void operation.
/// </summary>
public class InvoiceVoidResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Invoice? Invoice { get; set; }
}

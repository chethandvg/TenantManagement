using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Service for managing credit note operations.
/// </summary>
public interface ICreditNoteService
{
    /// <summary>
    /// Creates a credit note for an invoice with negative lines.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice to credit.</param>
    /// <param name="reason">The reason for the credit note.</param>
    /// <param name="lineItems">The line items to credit (references to invoice lines with amounts).</param>
    /// <param name="notes">Optional notes for the credit note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the created credit note or error message.</returns>
    Task<CreditNoteCreationResult> CreateCreditNoteAsync(
        Guid invoiceId,
        CreditNoteReason reason,
        IEnumerable<CreditNoteLineRequest> lineItems,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a credit note, making it final and immutable.
    /// </summary>
    /// <param name="creditNoteId">The ID of the credit note to issue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the issued credit note or error message.</returns>
    Task<CreditNoteIssueResult> IssueCreditNoteAsync(Guid creditNoteId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for creating a credit note line item.
/// </summary>
public class CreditNoteLineRequest
{
    public Guid InvoiceLineId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Result of credit note creation operation.
/// </summary>
public class CreditNoteCreationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public CreditNote? CreditNote { get; set; }
}

/// <summary>
/// Result of credit note issue operation.
/// </summary>
public class CreditNoteIssueResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public CreditNote? CreditNote { get; set; }
}

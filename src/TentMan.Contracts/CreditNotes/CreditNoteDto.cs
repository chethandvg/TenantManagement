using TentMan.Contracts.Enums;

namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// DTO for credit notes that reduce invoice balances for refunds, discounts, or corrections.
/// Credit notes are issued against invoices to adjust amounts without modifying the original invoice.
/// </summary>
public sealed class CreditNoteDto
{
    /// <summary>
    /// Gets the unique identifier for the credit note.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the organization ID this credit note belongs to.
    /// </summary>
    public Guid OrgId { get; init; }
    
    /// <summary>
    /// Gets the invoice ID this credit note is issued against.
    /// </summary>
    public Guid InvoiceId { get; init; }
    
    /// <summary>
    /// Gets the unique credit note number (e.g., "CN-202601-000008").
    /// Format: {Prefix}-{YYYYMM}-{Sequence}
    /// </summary>
    public string CreditNoteNumber { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the date when the credit note was created.
    /// </summary>
    public DateOnly CreditNoteDate { get; init; }
    
    /// <summary>
    /// Gets the reason for issuing this credit note.
    /// 1=InvoiceError, 2=Discount, 3=Refund, 4=Goodwill, 5=Adjustment, 99=Other
    /// </summary>
    public CreditNoteReason Reason { get; init; }
    
    /// <summary>
    /// Gets the total credit amount that will reduce the invoice balance.
    /// Sum of all credit note line items.
    /// </summary>
    public decimal TotalAmount { get; init; }
    
    /// <summary>
    /// Gets optional notes explaining why this credit note was issued.
    /// Example: "Maintenance charge was incorrect - should be ₹1500 not ₹2000".
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets the UTC timestamp when the credit note was applied to the invoice.
    /// Null if the credit note is still in draft status (not yet issued).
    /// When applied, the invoice balance is reduced by the credit note total.
    /// </summary>
    public DateTime? AppliedAtUtc { get; init; }
    
    /// <summary>
    /// Gets the line items that make up this credit note.
    /// Each line references an original invoice line and specifies the credit amount.
    /// </summary>
    public IEnumerable<CreditNoteLineDto> Lines { get; init; } = new List<CreditNoteLineDto>();
    
    /// <summary>
    /// Gets the row version for optimistic concurrency control.
    /// </summary>
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

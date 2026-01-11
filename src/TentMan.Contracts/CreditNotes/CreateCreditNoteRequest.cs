using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.CreditNotes;

/// <summary>
/// Line item in a credit note creation request.
/// References an original invoice line and specifies the amount to credit.
/// </summary>
public sealed class CreateCreditNoteLineRequest
{
    /// <summary>
    /// Gets the invoice line ID to credit against.
    /// Must reference a valid line from the invoice being credited.
    /// </summary>
    [Required]
    public Guid InvoiceLineId { get; init; }
    
    /// <summary>
    /// Gets the description for this credit line.
    /// Example: "Credit for maintenance overcharge" or "Partial refund for early termination".
    /// Maximum length is 500 characters.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the credit amount to apply.
    /// Must be greater than 0. Cannot exceed the original invoice line amount.
    /// </summary>
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
    
    /// <summary>
    /// Gets optional notes about this credit line.
    /// Maximum length is 1000 characters.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>
/// Request to create a credit note for an invoice.
/// Used to issue refunds, discounts, or corrections without modifying the original invoice.
/// </summary>
public sealed class CreateCreditNoteRequest
{
    /// <summary>
    /// Gets the invoice ID to issue the credit note against.
    /// Invoice must be in Issued, PartiallyPaid, or Paid status (not Draft or Voided).
    /// </summary>
    [Required]
    public Guid InvoiceId { get; init; }
    
    /// <summary>
    /// Gets the date for the credit note.
    /// Typically the current date.
    /// </summary>
    [Required]
    public DateOnly CreditNoteDate { get; init; }
    
    /// <summary>
    /// Gets the reason for issuing this credit note.
    /// 1=InvoiceError, 2=Discount, 3=Refund, 4=Goodwill, 5=Adjustment, 99=Other
    /// </summary>
    [Required]
    public CreditNoteReason Reason { get; init; }
    
    /// <summary>
    /// Gets optional notes explaining why this credit note is being issued.
    /// Example: "Customer complained about maintenance charge being too high".
    /// Maximum length is 2000 characters.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets the line items to credit.
    /// Must contain at least one line. Each line references an invoice line and specifies the credit amount.
    /// Total credit amount cannot exceed the invoice balance.
    /// </summary>
    [Required]
    [MinLength(1)]
    public IEnumerable<CreateCreditNoteLineRequest> Lines { get; init; } = new List<CreateCreditNoteLineRequest>();
}

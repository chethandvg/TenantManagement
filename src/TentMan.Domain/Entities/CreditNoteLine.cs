using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a line item on a credit note.
/// References the original invoice line being credited.
/// </summary>
public class CreditNoteLine : BaseEntity
{
    public CreditNoteLine()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid CreditNoteId { get; set; }
    public Guid InvoiceLineId { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public CreditNote CreditNote { get; set; } = null!;
    public InvoiceLine InvoiceLine { get; set; } = null!;
}

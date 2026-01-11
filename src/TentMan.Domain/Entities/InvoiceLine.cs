using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a line item on an invoice.
/// Each line represents a charge (rent, maintenance, utilities, etc.).
/// </summary>
public class InvoiceLine : BaseEntity
{
    public InvoiceLine()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
    }

    public Guid InvoiceId { get; set; }
    public Guid ChargeTypeId { get; set; }
    public int LineNumber { get; set; } // 1, 2, 3, etc.
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; } // Quantity * UnitPrice
    public decimal TaxRate { get; set; } // Percentage
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; } // Amount + TaxAmount
    public string? Notes { get; set; }
    
    // Source tracking for traceability
    public string? Source { get; set; } // e.g., "Rent", "RecurringCharge", "Utility"
    public Guid? SourceRefId { get; set; } // Reference to source entity (LeaseTermId, RecurringChargeId, UtilityStatementId)

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
    public ChargeType ChargeType { get; set; } = null!;
    public ICollection<UtilityStatement> UtilityStatements { get; set; } = new List<UtilityStatement>();
    public ICollection<CreditNoteLine> CreditNoteLines { get; set; } = new List<CreditNoteLine>();
}

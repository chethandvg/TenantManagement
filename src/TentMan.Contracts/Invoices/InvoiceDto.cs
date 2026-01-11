using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Invoices;

/// <summary>
/// DTO for invoices representing bills sent to tenants for rent, utilities, and other charges.
/// Contains the complete invoice with line items, amounts, and state information.
/// </summary>
public sealed class InvoiceDto
{
    /// <summary>
    /// Gets the unique identifier for the invoice.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the organization ID this invoice belongs to.
    /// </summary>
    public Guid OrgId { get; init; }
    
    /// <summary>
    /// Gets the lease ID this invoice is associated with.
    /// </summary>
    public Guid LeaseId { get; init; }
    
    /// <summary>
    /// Gets the unique invoice number (e.g., "INV-202601-000042").
    /// Format: {Prefix}-{YYYYMM}-{Sequence}
    /// </summary>
    public string InvoiceNumber { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the date when the invoice was generated.
    /// </summary>
    public DateOnly InvoiceDate { get; init; }
    
    /// <summary>
    /// Gets the date when payment is due.
    /// Calculated as InvoiceDate + PaymentTermDays from billing settings.
    /// </summary>
    public DateOnly DueDate { get; init; }
    
    /// <summary>
    /// Gets the current status of the invoice.
    /// 1=Draft, 2=Issued, 3=PartiallyPaid, 4=Paid, 5=Overdue, 6=Cancelled, 7=WrittenOff
    /// </summary>
    public InvoiceStatus Status { get; init; }
    
    /// <summary>
    /// Gets the start date of the billing period this invoice covers.
    /// </summary>
    public DateOnly BillingPeriodStart { get; init; }
    
    /// <summary>
    /// Gets the end date (inclusive) of the billing period this invoice covers.
    /// </summary>
    public DateOnly BillingPeriodEnd { get; init; }
    
    // Amounts
    
    /// <summary>
    /// Gets the subtotal amount before tax (sum of all line item amounts).
    /// </summary>
    public decimal SubTotal { get; init; }
    
    /// <summary>
    /// Gets the total tax amount applied to the invoice.
    /// </summary>
    public decimal TaxAmount { get; init; }
    
    /// <summary>
    /// Gets the total amount due (SubTotal + TaxAmount).
    /// This is the original total before any payments or credits.
    /// </summary>
    public decimal TotalAmount { get; init; }
    
    /// <summary>
    /// Gets the amount already paid by the tenant.
    /// Updated when payments are recorded.
    /// </summary>
    public decimal PaidAmount { get; init; }
    
    /// <summary>
    /// Gets the remaining balance due (TotalAmount - PaidAmount - Credits).
    /// When this reaches zero, the invoice status changes to Paid.
    /// </summary>
    public decimal BalanceAmount { get; init; }
    
    // Dates
    
    /// <summary>
    /// Gets the UTC timestamp when the invoice was issued (changed from Draft to Issued).
    /// Null if the invoice is still in Draft status.
    /// </summary>
    public DateTime? IssuedAtUtc { get; init; }
    
    /// <summary>
    /// Gets the UTC timestamp when the invoice was fully paid.
    /// Null if the invoice is not yet fully paid.
    /// </summary>
    public DateTime? PaidAtUtc { get; init; }
    
    /// <summary>
    /// Gets the UTC timestamp when the invoice was voided/cancelled.
    /// Null if the invoice has not been voided.
    /// </summary>
    public DateTime? VoidedAtUtc { get; init; }
    
    // Additional info
    
    /// <summary>
    /// Gets optional internal notes about this invoice.
    /// Not displayed to tenants.
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets custom payment instructions to display on the invoice.
    /// Example: "Please pay via bank transfer to Account #12345".
    /// Inherited from lease billing settings.
    /// </summary>
    public string? PaymentInstructions { get; init; }
    
    /// <summary>
    /// Gets the reason why the invoice was voided.
    /// Only populated if Status is Cancelled and VoidedAtUtc is not null.
    /// </summary>
    public string? VoidReason { get; init; }
    
    /// <summary>
    /// Gets the line items (charges) that make up this invoice.
    /// Each line represents a charge type (rent, utilities, maintenance, etc.).
    /// </summary>
    public IEnumerable<InvoiceLineDto> Lines { get; init; } = new List<InvoiceLineDto>();
    
    /// <summary>
    /// Gets the row version for optimistic concurrency control.
    /// </summary>
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

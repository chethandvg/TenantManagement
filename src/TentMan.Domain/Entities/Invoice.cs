using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents an invoice for a lease.
/// Contains line items for various charges.
/// </summary>
public class Invoice : BaseEntity
{
    public Invoice()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        Status = InvoiceStatus.Draft;
    }

    public Guid OrgId { get; set; }
    public Guid LeaseId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateOnly BillingPeriodStart { get; set; }
    public DateOnly BillingPeriodEnd { get; set; }
    
    // Amounts
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    
    // Dates
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    
    // Additional info
    public string? Notes { get; set; }
    public string? PaymentInstructions { get; set; }
    public string? VoidReason { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Lease Lease { get; set; } = null!;
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
}

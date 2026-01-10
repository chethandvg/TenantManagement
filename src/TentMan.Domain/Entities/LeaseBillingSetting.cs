using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents billing settings for a lease.
/// Controls billing behavior like billing day, payment terms, etc.
/// </summary>
public class LeaseBillingSetting : BaseEntity
{
    public LeaseBillingSetting()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        BillingDay = 1;
        PaymentTermDays = 0;
        GenerateInvoiceAutomatically = true;
    }

    public Guid LeaseId { get; set; }
    public byte BillingDay { get; set; } // 1-28, day of month to generate invoice
    public short PaymentTermDays { get; set; } // Payment due days after invoice
    public bool GenerateInvoiceAutomatically { get; set; }
    public string? InvoicePrefix { get; set; } // Custom invoice number prefix
    public string? PaymentInstructions { get; set; } // Bank details, UPI, etc.
    public string? Notes { get; set; }

    // Navigation properties
    public Lease Lease { get; set; } = null!;
}

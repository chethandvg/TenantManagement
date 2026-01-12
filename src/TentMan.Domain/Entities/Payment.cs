using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a payment made against an invoice.
/// Tracks payment mode, amount, status, and associated metadata.
/// </summary>
public class Payment : BaseEntity
{
    public Payment()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        Status = PaymentStatus.Pending;
    }

    public Guid OrgId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid LeaseId { get; set; }
    
    /// <summary>
    /// Payment mode/method used
    /// </summary>
    public PaymentMode PaymentMode { get; set; }
    
    /// <summary>
    /// Current status of the payment
    /// </summary>
    public PaymentStatus Status { get; set; }
    
    /// <summary>
    /// Amount paid in this payment
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Date when payment was received/completed
    /// </summary>
    public DateTime PaymentDateUtc { get; set; }
    
    /// <summary>
    /// Transaction reference number (bank ref, UPI ref, gateway transaction ID, etc.)
    /// </summary>
    public string? TransactionReference { get; set; }
    
    /// <summary>
    /// Who received/recorded the payment (UserId or system identifier)
    /// </summary>
    public string ReceivedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Who made the payment (tenant name, party name)
    /// </summary>
    public string? PayerName { get; set; }
    
    /// <summary>
    /// Optional notes about the payment
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Metadata for online payments (gateway response, etc.) - stored as JSON
    /// </summary>
    public string? PaymentMetadata { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public Lease Lease { get; set; } = null!;
    public PaymentConfirmationRequest? PaymentConfirmationRequest { get; set; }
}

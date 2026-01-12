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
        PaymentType = PaymentType.Rent; // Default to Rent for backward compatibility
    }

    public Guid OrgId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid LeaseId { get; set; }
    
    /// <summary>
    /// Type/category of payment (Rent, Utility, Invoice, Deposit, etc.)
    /// </summary>
    public PaymentType PaymentType { get; set; }
    
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
    /// Transaction reference number (bank ref, UPI ref, check number, etc.)
    /// </summary>
    public string? TransactionReference { get; set; }
    
    /// <summary>
    /// Payment gateway transaction ID (for online payments via gateway)
    /// </summary>
    public string? GatewayTransactionId { get; set; }
    
    /// <summary>
    /// Name of payment gateway used (e.g., "Razorpay", "Stripe", "PayPal")
    /// </summary>
    public string? GatewayName { get; set; }
    
    /// <summary>
    /// Full gateway response stored as JSON for reference and reconciliation
    /// </summary>
    public string? GatewayResponse { get; set; }
    
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
    
    /// <summary>
    /// Optional reference to utility statement/bill (for utility payments)
    /// </summary>
    public Guid? UtilityStatementId { get; set; }
    
    /// <summary>
    /// Optional reference to deposit transaction (for deposit payments)
    /// </summary>
    public Guid? DepositTransactionId { get; set; }
    
    /// <summary>
    /// Country/region code for BBPS or international billing systems (ISO 3166-1 alpha-2, e.g., "IN", "US")
    /// </summary>
    public string? CountryCode { get; set; }
    
    /// <summary>
    /// Biller ID for BBPS or similar utility billing systems
    /// </summary>
    public string? BillerId { get; set; }
    
    /// <summary>
    /// Customer/consumer ID for utility billing
    /// </summary>
    public string? ConsumerId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public Lease Lease { get; set; } = null!;
    public UtilityStatement? UtilityStatement { get; set; }
    public DepositTransaction? DepositTransaction { get; set; }
    public PaymentConfirmationRequest? PaymentConfirmationRequest { get; set; }
    public ICollection<PaymentStatusHistory> StatusHistory { get; set; } = new List<PaymentStatusHistory>();
    public ICollection<PaymentAttachment> Attachments { get; set; } = new List<PaymentAttachment>();
}

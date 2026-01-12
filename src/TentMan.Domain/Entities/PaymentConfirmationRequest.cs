using TentMan.Domain.Common;
using TentMan.Contracts.Enums;

namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a tenant-initiated request to confirm a cash payment.
/// Allows tenants to submit proof of cash payment for owner review.
/// </summary>
public class PaymentConfirmationRequest : BaseEntity
{
    public PaymentConfirmationRequest()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        Status = PaymentConfirmationStatus.Pending;
    }

    public Guid OrgId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid LeaseId { get; set; }
    
    /// <summary>
    /// Amount claimed to be paid
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Date when tenant claims to have made the payment
    /// </summary>
    public DateTime PaymentDateUtc { get; set; }
    
    /// <summary>
    /// Receipt number or reference provided by tenant
    /// </summary>
    public string? ReceiptNumber { get; set; }
    
    /// <summary>
    /// Notes from tenant about the payment
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// ID of uploaded proof file (receipt photo, etc.)
    /// </summary>
    public Guid? ProofFileId { get; set; }
    
    /// <summary>
    /// Current status of the confirmation request
    /// </summary>
    public PaymentConfirmationStatus Status { get; set; }
    
    /// <summary>
    /// When the request was reviewed (confirmed or rejected)
    /// </summary>
    public DateTime? ReviewedAtUtc { get; set; }
    
    /// <summary>
    /// Who reviewed the request (owner/manager user ID)
    /// </summary>
    public string? ReviewedBy { get; set; }
    
    /// <summary>
    /// Response/notes from owner when confirming or rejecting
    /// </summary>
    public string? ReviewResponse { get; set; }
    
    /// <summary>
    /// Payment ID created when request is confirmed
    /// </summary>
    public Guid? PaymentId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public Lease Lease { get; set; } = null!;
    public FileMetadata? ProofFile { get; set; }
    public Payment? Payment { get; set; }
}

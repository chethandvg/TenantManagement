using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Payments;

/// <summary>
/// DTO for payment confirmation request information.
/// </summary>
public class PaymentConfirmationRequestDto
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid LeaseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDateUtc { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public Guid? ProofFileId { get; set; }
    public string? ProofFileUrl { get; set; }
    public string? ProofFileName { get; set; }
    public PaymentConfirmationStatus Status { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewResponse { get; set; }
    public Guid? PaymentId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Request to create a payment confirmation request (tenant-initiated).
/// </summary>
public class CreatePaymentConfirmationRequest
{
    /// <summary>
    /// Invoice ID for which payment is being confirmed
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Amount paid
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date when payment was made
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Receipt number or reference
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Notes about the payment
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request to confirm a payment confirmation request (owner-initiated).
/// </summary>
public class ConfirmPaymentRequest
{
    /// <summary>
    /// Response notes from owner
    /// </summary>
    public string? ReviewResponse { get; set; }
}

/// <summary>
/// Request to reject a payment confirmation request (owner-initiated).
/// </summary>
public class RejectPaymentRequest
{
    /// <summary>
    /// Reason for rejection
    /// </summary>
    public string ReviewResponse { get; set; } = string.Empty;
}

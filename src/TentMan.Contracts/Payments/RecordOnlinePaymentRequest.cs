using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Payments;

/// <summary>
/// Request to record an online payment.
/// This is a stub for future payment gateway integration.
/// </summary>
public class RecordOnlinePaymentRequest
{
    /// <summary>
    /// Payment mode (Online, UPI, BankTransfer, etc.)
    /// </summary>
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Online;

    /// <summary>
    /// Amount paid
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date when payment was made
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Transaction reference from payment gateway/bank
    /// </summary>
    public string TransactionReference { get; set; } = string.Empty;

    /// <summary>
    /// Name of the person who made the payment
    /// </summary>
    public string? PayerName { get; set; }

    /// <summary>
    /// Optional notes about the payment
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Optional metadata (JSON) from payment gateway
    /// </summary>
    public string? PaymentMetadata { get; set; }
}

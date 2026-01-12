using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Payments;

/// <summary>
/// Request to record a cash payment.
/// Cash payments are immediately marked as completed.
/// </summary>
public class RecordCashPaymentRequest
{
    /// <summary>
    /// Amount paid in cash
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date when cash was received
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Name of the person who made the payment
    /// </summary>
    public string? PayerName { get; set; }

    /// <summary>
    /// Optional receipt number or reference
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Optional notes about the payment
    /// </summary>
    public string? Notes { get; set; }
}

using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Payments;

/// <summary>
/// Request model for recording a payment via unified /api/payments endpoint.
/// </summary>
public class RecordUnifiedPaymentRequest
{
    public Guid InvoiceId { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Rent;
    public PaymentMode PaymentMode { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? TransactionReference { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? GatewayName { get; set; }
    public string? PayerName { get; set; }
    public string? Notes { get; set; }
    public string? PaymentMetadata { get; set; }
}

/// <summary>
/// Request model for confirming a payment.
/// </summary>
public class ConfirmPaymentDirectRequest
{
    public string? Notes { get; set; }
}

/// <summary>
/// Request model for rejecting a payment.
/// </summary>
public class RejectPaymentDirectRequest
{
    public string Reason { get; set; } = string.Empty;
}

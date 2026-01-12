using MediatR;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Billing.Commands.RecordPayment;

/// <summary>
/// Command to record a cash payment for an invoice.
/// Cash payments are immediately marked as completed.
/// </summary>
public class RecordCashPaymentCommand : IRequest<RecordPaymentResult>
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PayerName { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Command to record an online payment for an invoice.
/// This is a stub for future payment gateway integration.
/// </summary>
public class RecordOnlinePaymentCommand : IRequest<RecordPaymentResult>
{
    public Guid InvoiceId { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Online;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string? PayerName { get; set; }
    public string? Notes { get; set; }
    public string? PaymentMetadata { get; set; }
}

/// <summary>
/// Result of recording a payment.
/// </summary>
public class RecordPaymentResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal? InvoiceBalanceAmount { get; set; }
}

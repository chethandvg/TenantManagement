using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Payments;

/// <summary>
/// DTO for payment information.
/// </summary>
public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid LeaseId { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDateUtc { get; set; }
    public string? TransactionReference { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string? PayerName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

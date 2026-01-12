using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Payments;

/// <summary>
/// DTO for payment status history record.
/// </summary>
public class PaymentStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public PaymentStatus FromStatus { get; set; }
    public PaymentStatus ToStatus { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Metadata { get; set; }
}

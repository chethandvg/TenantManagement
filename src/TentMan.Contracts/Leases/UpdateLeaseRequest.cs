using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Leases;

/// <summary>
/// Request to update lease header terms.
/// </summary>
public class UpdateLeaseRequest
{
    public string? LeaseNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public byte RentDueDay { get; set; }
    public byte GraceDays { get; set; }
    public short? NoticePeriodDays { get; set; }
    public LateFeeType LateFeeType { get; set; }
    public decimal? LateFeeValue { get; set; }
    public string? PaymentMethodNote { get; set; }
    public string? TermsText { get; set; }
    public bool IsAutoRenew { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

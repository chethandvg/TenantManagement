using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Leases;

/// <summary>
/// Request to create a new lease (draft).
/// </summary>
public class CreateLeaseRequest
{
    public Guid UnitId { get; set; }
    public string? LeaseNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public byte RentDueDay { get; set; } = 1;
    public byte GraceDays { get; set; } = 0;
    public short? NoticePeriodDays { get; set; }
    public LateFeeType LateFeeType { get; set; } = LateFeeType.None;
    public decimal? LateFeeValue { get; set; }
    public string? PaymentMethodNote { get; set; }
    public string? TermsText { get; set; }
    public bool IsAutoRenew { get; set; } = false;
}

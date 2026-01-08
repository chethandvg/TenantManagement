using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Leases;

/// <summary>
/// DTO for lease details with related data.
/// </summary>
public class LeaseDetailDto
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public Guid UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? LeaseNumber { get; set; }
    public LeaseStatus Status { get; set; }
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
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<LeasePartyDto> Parties { get; set; } = new();
    public List<LeaseTermDto> Terms { get; set; } = new();
    public List<DepositTransactionDto> DepositTransactions { get; set; } = new();
    public decimal TotalDepositCollected { get; set; }
    public decimal DepositBalance { get; set; }
}

public class LeasePartyDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public LeasePartyRole Role { get; set; }
    public bool IsResponsibleForPayment { get; set; }
    public DateOnly? MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
}

public class LeaseTermDto
{
    public Guid Id { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal? MaintenanceCharge { get; set; }
    public decimal? OtherFixedCharge { get; set; }
    public EscalationType EscalationType { get; set; }
    public decimal? EscalationValue { get; set; }
    public short? EscalationEveryMonths { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public class DepositTransactionDto
{
    public Guid Id { get; set; }
    public DepositTransactionType TxnType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TxnDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

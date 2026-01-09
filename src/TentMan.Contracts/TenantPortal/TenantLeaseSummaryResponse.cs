using TentMan.Contracts.Enums;

namespace TentMan.Contracts.TenantPortal;

/// <summary>
/// Response DTO for tenant lease summary containing lease, unit, and financial details.
/// </summary>
public class TenantLeaseSummaryResponse
{
    public Guid LeaseId { get; set; }
    public string? LeaseNumber { get; set; }
    public LeaseStatus Status { get; set; }
    
    // Lease dates and terms
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public byte RentDueDay { get; set; }
    public byte GraceDays { get; set; }
    public short? NoticePeriodDays { get; set; }
    public bool IsAutoRenew { get; set; }
    public string? TermsText { get; set; }
    
    // Payment information
    public string? PaymentMethodNote { get; set; }
    public LateFeeType LateFeeType { get; set; }
    public decimal? LateFeeValue { get; set; }
    
    // Unit details
    public Guid UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public string? BuildingName { get; set; }
    public string? BuildingAddress { get; set; }
    
    // Current financial terms
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal? MaintenanceCharge { get; set; }
    public decimal? OtherFixedCharge { get; set; }
    
    // Deposit information
    public decimal TotalDepositCollected { get; set; }
    public decimal DepositBalance { get; set; }
    
    // Lease parties (other tenants/occupants)
    public List<TenantLeasePartyDto> LeaseParties { get; set; } = new();
    
    // Historical financial terms
    public List<TenantLeaseTermDto> TermsHistory { get; set; } = new();
    
    // Deposit transaction history
    public List<TenantDepositTransactionDto> DepositTransactions { get; set; } = new();
}

public class TenantLeasePartyDto
{
    public string FullName { get; set; } = string.Empty;
    public LeasePartyRole Role { get; set; }
    public bool IsResponsibleForPayment { get; set; }
    public DateOnly? MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
}

public class TenantLeaseTermDto
{
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal? MaintenanceCharge { get; set; }
    public decimal? OtherFixedCharge { get; set; }
    public bool IsActive { get; set; }
}

public class TenantDepositTransactionDto
{
    public DepositTransactionType TxnType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TxnDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

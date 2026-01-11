using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for utility statements.
/// </summary>
public sealed class UtilityStatementDto
{
    public Guid Id { get; init; }
    public Guid LeaseId { get; init; }
    public UtilityType UtilityType { get; init; }
    public DateOnly BillingPeriodStart { get; init; }
    public DateOnly BillingPeriodEnd { get; init; }
    public bool IsMeterBased { get; init; }
    
    // Meter-based fields
    public Guid? UtilityRatePlanId { get; init; }
    public decimal? PreviousReading { get; init; }
    public decimal? CurrentReading { get; init; }
    public decimal? UnitsConsumed { get; init; }
    public decimal? CalculatedAmount { get; init; }
    
    // Amount-based fields
    public decimal? DirectBillAmount { get; init; }
    
    // Final amounts
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public Guid? InvoiceLineId { get; init; }
    public int Version { get; init; }
    public bool IsFinal { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

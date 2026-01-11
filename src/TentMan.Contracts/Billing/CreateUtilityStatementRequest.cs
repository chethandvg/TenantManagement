using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to create a utility statement.
/// </summary>
public sealed class CreateUtilityStatementRequest
{
    [Required]
    public Guid LeaseId { get; init; }
    
    [Required]
    public UtilityType UtilityType { get; init; }
    
    [Required]
    public DateOnly BillingPeriodStart { get; init; }
    
    [Required]
    public DateOnly BillingPeriodEnd { get; init; }
    
    public bool IsMeterBased { get; init; }
    
    // Meter-based fields
    public Guid? UtilityRatePlanId { get; init; }
    
    [Range(0, double.MaxValue)]
    public decimal? PreviousReading { get; init; }
    
    [Range(0, double.MaxValue)]
    public decimal? CurrentReading { get; init; }
    
    // Amount-based fields
    [Range(0, double.MaxValue)]
    public decimal? DirectBillAmount { get; init; }
    
    [MaxLength(2000)]
    public string? Notes { get; init; }
}

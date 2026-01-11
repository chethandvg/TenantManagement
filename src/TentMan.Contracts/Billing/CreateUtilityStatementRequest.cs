using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// Request to create a utility statement for recording utility consumption and billing.
/// Supports both meter-based billing (with readings and rate plans) and amount-based billing (direct amount from provider).
/// </summary>
public sealed class CreateUtilityStatementRequest
{
    /// <summary>
    /// Gets the lease ID this utility statement is associated with.
    /// </summary>
    [Required]
    public Guid LeaseId { get; init; }
    
    /// <summary>
    /// Gets the type of utility being billed (Electricity, Water, or Gas).
    /// </summary>
    [Required]
    public UtilityType UtilityType { get; init; }
    
    /// <summary>
    /// Gets the start date of the billing period for this utility statement.
    /// </summary>
    [Required]
    public DateOnly BillingPeriodStart { get; init; }
    
    /// <summary>
    /// Gets the end date of the billing period for this utility statement (inclusive).
    /// Must be after the start date.
    /// </summary>
    [Required]
    public DateOnly BillingPeriodEnd { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this statement uses meter readings for calculation.
    /// When true, provide PreviousReading, CurrentReading, and UtilityRatePlanId.
    /// When false, provide DirectBillAmount.
    /// </summary>
    public bool IsMeterBased { get; init; }
    
    // Meter-based fields
    
    /// <summary>
    /// Gets the utility rate plan ID to use for meter-based calculations.
    /// Required when IsMeterBased is true. The rate plan defines tiered pricing slabs.
    /// </summary>
    public Guid? UtilityRatePlanId { get; init; }
    
    /// <summary>
    /// Gets the previous meter reading (starting reading for the period).
    /// Required when IsMeterBased is true. Must be non-negative.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? PreviousReading { get; init; }
    
    /// <summary>
    /// Gets the current meter reading (ending reading for the period).
    /// Required when IsMeterBased is true. Must be greater than or equal to PreviousReading.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? CurrentReading { get; init; }
    
    // Amount-based fields
    
    /// <summary>
    /// Gets the direct bill amount from the utility provider.
    /// Required when IsMeterBased is false. Must be non-negative.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? DirectBillAmount { get; init; }
    
    /// <summary>
    /// Gets optional notes about this utility statement.
    /// Example: "January 2026 electricity bill - meter reading taken on 31st".
    /// Maximum length is 2000 characters.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; init; }
}

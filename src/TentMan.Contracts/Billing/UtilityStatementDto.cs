using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for utility statements that record utility consumption and billing.
/// Supports both meter-based billing (with readings) and amount-based billing (direct bill amount).
/// </summary>
public sealed class UtilityStatementDto
{
    /// <summary>
    /// Gets the unique identifier for the utility statement.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the lease ID this utility statement is associated with.
    /// </summary>
    public Guid LeaseId { get; init; }
    
    /// <summary>
    /// Gets the type of utility (Electricity, Water, or Gas).
    /// </summary>
    public UtilityType UtilityType { get; init; }
    
    /// <summary>
    /// Gets the start date of the billing period for this utility statement.
    /// </summary>
    public DateOnly BillingPeriodStart { get; init; }
    
    /// <summary>
    /// Gets the end date of the billing period for this utility statement (inclusive).
    /// </summary>
    public DateOnly BillingPeriodEnd { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this statement uses meter readings for calculation.
    /// When true, uses PreviousReading, CurrentReading, and UtilityRatePlan.
    /// When false, uses DirectBillAmount.
    /// </summary>
    public bool IsMeterBased { get; init; }
    
    // Meter-based fields
    
    /// <summary>
    /// Gets the utility rate plan ID used for meter-based calculations.
    /// Only populated when IsMeterBased is true.
    /// </summary>
    public Guid? UtilityRatePlanId { get; init; }
    
    /// <summary>
    /// Gets the previous meter reading.
    /// Only populated when IsMeterBased is true.
    /// </summary>
    public decimal? PreviousReading { get; init; }
    
    /// <summary>
    /// Gets the current meter reading.
    /// Only populated when IsMeterBased is true.
    /// </summary>
    public decimal? CurrentReading { get; init; }
    
    /// <summary>
    /// Gets the calculated units consumed (CurrentReading - PreviousReading).
    /// Only populated when IsMeterBased is true.
    /// </summary>
    public decimal? UnitsConsumed { get; init; }
    
    /// <summary>
    /// Gets the calculated amount based on the utility rate plan and units consumed.
    /// Only populated when IsMeterBased is true.
    /// </summary>
    public decimal? CalculatedAmount { get; init; }
    
    // Amount-based fields
    
    /// <summary>
    /// Gets the direct bill amount from the utility provider.
    /// Only populated when IsMeterBased is false (amount-based billing).
    /// </summary>
    public decimal? DirectBillAmount { get; init; }
    
    // Final amounts
    
    /// <summary>
    /// Gets the total amount to be billed for this utility statement.
    /// Equals CalculatedAmount for meter-based or DirectBillAmount for amount-based.
    /// </summary>
    public decimal TotalAmount { get; init; }
    
    /// <summary>
    /// Gets optional notes about this utility statement (e.g., "January 2026 electricity bill").
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// Gets the invoice line ID if this statement has been billed on an invoice.
    /// Null if not yet billed.
    /// </summary>
    public Guid? InvoiceLineId { get; init; }
    
    /// <summary>
    /// Gets the version number of this utility statement.
    /// Increments with each correction. Final statements must have version greater than 0.
    /// </summary>
    public int Version { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this is the final version of the statement.
    /// Only one final statement is allowed per lease/utility type/billing period.
    /// Draft versions can be created for corrections before finalizing.
    /// </summary>
    public bool IsFinal { get; init; }
    
    /// <summary>
    /// Gets the row version for optimistic concurrency control.
    /// </summary>
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Service for calculating recurring charges for a billing period.
/// Handles proration for charges that start or end mid-period.
/// </summary>
public interface IRecurringChargeCalculationService
{
    /// <summary>
    /// Calculates recurring charges for a specific billing period.
    /// </summary>
    /// <param name="leaseId">The lease ID</param>
    /// <param name="billingPeriodStart">Start of the billing period</param>
    /// <param name="billingPeriodEnd">End of the billing period (inclusive)</param>
    /// <param name="prorationMethod">Method to use for proration calculations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Calculation result with line items for each charge</returns>
    Task<RecurringChargeCalculationResult> CalculateChargesAsync(
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of recurring charge calculation for a billing period.
/// </summary>
public class RecurringChargeCalculationResult
{
    public decimal TotalAmount { get; set; }
    public List<RecurringChargeLineItem> LineItems { get; set; } = new();
}

/// <summary>
/// Represents a single line item for a recurring charge.
/// </summary>
public class RecurringChargeLineItem
{
    public Guid ChargeId { get; set; }
    public string ChargeDescription { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal FullAmount { get; set; }
    public decimal Amount { get; set; }
    public bool IsProrated { get; set; }
    public BillingFrequency Frequency { get; set; }
}

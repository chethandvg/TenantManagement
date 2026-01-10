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
    public decimal TotalAmount { get; init; }
    public List<RecurringChargeLineItem> LineItems { get; init; } = new();
}

/// <summary>
/// Represents a single line item for a recurring charge.
/// </summary>
public class RecurringChargeLineItem
{
    public Guid ChargeId { get; init; }
    public Guid ChargeTypeId { get; init; }
    public string ChargeDescription { get; init; } = string.Empty;
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal FullAmount { get; init; }
    public decimal Amount { get; init; }
    public bool IsProrated { get; init; }
    public BillingFrequency Frequency { get; init; }
}

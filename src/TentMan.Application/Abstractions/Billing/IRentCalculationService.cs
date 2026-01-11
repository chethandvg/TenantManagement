using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Service for calculating rent charges for a billing period.
/// Handles multiple lease terms and mid-period changes with proration.
/// </summary>
public interface IRentCalculationService
{
    /// <summary>
    /// Calculates rent for a specific billing period.
    /// </summary>
    /// <param name="leaseId">The lease ID</param>
    /// <param name="billingPeriodStart">Start of the billing period</param>
    /// <param name="billingPeriodEnd">End of the billing period (inclusive)</param>
    /// <param name="prorationMethod">Method to use for proration calculations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Calculation result with line items for each term period</returns>
    Task<RentCalculationResult> CalculateRentAsync(
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of rent calculation for a billing period.
/// </summary>
public class RentCalculationResult
{
    public decimal TotalAmount { get; init; }
    public List<RentLineItem> LineItems { get; init; } = new();
}

/// <summary>
/// Represents a single line item in the rent calculation.
/// </summary>
public class RentLineItem
{
    public Guid LeaseTermId { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public decimal FullMonthlyRent { get; init; }
    public decimal Amount { get; init; }
    public bool IsProrated { get; init; }
    public string Description { get; init; } = string.Empty;
}

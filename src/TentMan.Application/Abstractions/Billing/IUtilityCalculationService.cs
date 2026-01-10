using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Service for calculating utility charges.
/// Supports amount-based, meter-based with flat rate, and meter-based with slabs.
/// </summary>
public interface IUtilityCalculationService
{
    /// <summary>
    /// Calculates utility charge based on a direct billing amount.
    /// </summary>
    /// <param name="amount">The direct bill amount from utility provider</param>
    /// <param name="utilityType">Type of utility</param>
    /// <returns>Calculation result</returns>
    UtilityCalculationResult CalculateAmountBased(
        decimal amount,
        UtilityType utilityType);

    /// <summary>
    /// Calculates utility charge based on meter consumption with a flat rate.
    /// </summary>
    /// <param name="unitsConsumed">Number of units consumed</param>
    /// <param name="ratePerUnit">Rate per unit</param>
    /// <param name="fixedCharge">Optional fixed charge</param>
    /// <param name="utilityType">Type of utility</param>
    /// <returns>Calculation result</returns>
    UtilityCalculationResult CalculateMeterBasedFlatRate(
        decimal unitsConsumed,
        decimal ratePerUnit,
        decimal fixedCharge,
        UtilityType utilityType);

    /// <summary>
    /// Calculates utility charge based on meter consumption with slab-based rates.
    /// </summary>
    /// <param name="unitsConsumed">Number of units consumed</param>
    /// <param name="ratePlanId">ID of the utility rate plan with slabs</param>
    /// <param name="utilityType">Type of utility</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Calculation result</returns>
    Task<UtilityCalculationResult> CalculateMeterBasedSlabsAsync(
        decimal unitsConsumed,
        Guid ratePlanId,
        UtilityType utilityType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of utility calculation.
/// </summary>
public class UtilityCalculationResult
{
    public UtilityType UtilityType { get; init; }
    public bool IsMeterBased { get; init; }
    public decimal? UnitsConsumed { get; init; }
    public decimal TotalAmount { get; init; }
    public List<UtilitySlabLineItem> SlabBreakdown { get; init; } = new();
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Represents a slab calculation in the breakdown.
/// </summary>
public class UtilitySlabLineItem
{
    public decimal FromUnits { get; init; }
    public decimal ToUnits { get; init; }
    public decimal UnitsInSlab { get; init; }
    public decimal RatePerUnit { get; init; }
    public decimal Amount { get; init; }
    public decimal? FixedCharge { get; init; }
}

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Interface for calculating prorated amounts based on a date period.
/// </summary>
public interface IProrationCalculator
{
    /// <summary>
    /// Calculates the prorated amount for a given period.
    /// </summary>
    /// <param name="fullAmount">The full amount for the billing period (e.g., monthly charge)</param>
    /// <param name="startDate">Start date of the period to calculate</param>
    /// <param name="endDate">End date of the period to calculate (inclusive)</param>
    /// <param name="billingPeriodStart">Start of the full billing period</param>
    /// <param name="billingPeriodEnd">End of the full billing period (inclusive)</param>
    /// <returns>The prorated amount</returns>
    decimal CalculateProration(
        decimal fullAmount,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd);
}

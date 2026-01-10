using TentMan.Application.Abstractions.Billing;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Proration calculator that uses fixed 30 days per month.
/// Formula: (DaysUsed / 30) * FullAmount
/// </summary>
public class ThirtyDayMonthCalculator : IProrationCalculator
{
    private const int DaysPerMonth = 30;

    /// <inheritdoc/>
    public decimal CalculateProration(
        decimal fullAmount,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd)
    {
        if (fullAmount < 0)
            throw new ArgumentException("Full amount cannot be negative", nameof(fullAmount));
        
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date", nameof(endDate));
        
        if (billingPeriodEnd < billingPeriodStart)
            throw new ArgumentException("Billing period end cannot be before start", nameof(billingPeriodEnd));

        // Ensure dates are within the billing period
        var effectiveStartDate = startDate < billingPeriodStart ? billingPeriodStart : startDate;
        var effectiveEndDate = endDate > billingPeriodEnd ? billingPeriodEnd : endDate;

        // If the effective period is invalid, return 0
        if (effectiveEndDate < effectiveStartDate)
            return 0m;

        // Calculate actual days (inclusive)
        var daysUsed = effectiveEndDate.DayNumber - effectiveStartDate.DayNumber + 1;

        // Calculate prorated amount using 30-day month
        var proratedAmount = (daysUsed / (decimal)DaysPerMonth) * fullAmount;
        
        return Math.Round(proratedAmount, 2, MidpointRounding.AwayFromZero);
    }
}

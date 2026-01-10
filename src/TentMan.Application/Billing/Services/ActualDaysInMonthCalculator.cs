using TentMan.Application.Abstractions.Billing;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Proration calculator that uses actual number of days in a month (28-31).
/// Formula: (DaysUsed / TotalDaysInPeriod) * FullAmount
/// </summary>
public class ActualDaysInMonthCalculator : IProrationCalculator
{
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
        var totalDaysInPeriod = billingPeriodEnd.DayNumber - billingPeriodStart.DayNumber + 1;

        if (totalDaysInPeriod <= 0)
            return 0m;

        // Calculate prorated amount
        var proratedAmount = (daysUsed / (decimal)totalDaysInPeriod) * fullAmount;
        
        return Math.Round(proratedAmount, 2, MidpointRounding.AwayFromZero);
    }
}

using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Service for calculating recurring charges with proration support.
/// </summary>
public class RecurringChargeCalculationService : IRecurringChargeCalculationService
{
    private readonly ILeaseRecurringChargeRepository _recurringChargeRepository;
    private readonly ActualDaysInMonthCalculator _actualDaysCalculator;
    private readonly ThirtyDayMonthCalculator _thirtyDayCalculator;

    public RecurringChargeCalculationService(ILeaseRecurringChargeRepository recurringChargeRepository)
    {
        _recurringChargeRepository = recurringChargeRepository;
        _actualDaysCalculator = new ActualDaysInMonthCalculator();
        _thirtyDayCalculator = new ThirtyDayMonthCalculator();
    }

    /// <inheritdoc/>
    public async Task<RecurringChargeCalculationResult> CalculateChargesAsync(
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default)
    {
        if (billingPeriodEnd < billingPeriodStart)
            throw new ArgumentException("Billing period end cannot be before start", nameof(billingPeriodEnd));

        var result = new RecurringChargeCalculationResult();
        var calculator = GetCalculator(prorationMethod);

        // Get active recurring charges for the lease
        var recurringCharges = await _recurringChargeRepository.GetActiveByLeaseIdAsync(leaseId, cancellationToken);

        foreach (var charge in recurringCharges)
        {
            // Only process monthly charges for now (can extend for other frequencies)
            if (charge.Frequency != BillingFrequency.Monthly)
                continue;

            // Check if charge is applicable for this billing period
            if (charge.StartDate > billingPeriodEnd)
                continue; // Charge starts after billing period

            if (charge.EndDate.HasValue && charge.EndDate < billingPeriodStart)
                continue; // Charge ended before billing period

            // Calculate effective period
            var effectiveStart = charge.StartDate > billingPeriodStart 
                ? charge.StartDate 
                : billingPeriodStart;
            
            var effectiveEnd = charge.EndDate.HasValue && charge.EndDate < billingPeriodEnd
                ? charge.EndDate.Value
                : billingPeriodEnd;

            if (effectiveEnd < effectiveStart)
                continue; // Invalid period

            var isProrated = effectiveStart != billingPeriodStart || effectiveEnd != billingPeriodEnd;
            var amount = calculator.CalculateProration(
                charge.Amount,
                effectiveStart,
                effectiveEnd,
                billingPeriodStart,
                billingPeriodEnd);

            var lineItem = new RecurringChargeLineItem
            {
                ChargeId = charge.Id,
                ChargeDescription = charge.Description,
                PeriodStart = effectiveStart,
                PeriodEnd = effectiveEnd,
                FullAmount = charge.Amount,
                Amount = amount,
                IsProrated = isProrated,
                Frequency = charge.Frequency
            };

            result.LineItems.Add(lineItem);
            result.TotalAmount += amount;
        }

        return result;
    }

    private IProrationCalculator GetCalculator(ProrationMethod method)
    {
        return method switch
        {
            ProrationMethod.ActualDaysInMonth => _actualDaysCalculator,
            ProrationMethod.ThirtyDayMonth => _thirtyDayCalculator,
            _ => throw new ArgumentException($"Unknown proration method: {method}", nameof(method))
        };
    }
}

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

        var calculator = GetCalculator(prorationMethod);

        // Get active recurring charges for the lease
        var recurringCharges = await _recurringChargeRepository.GetActiveByLeaseIdAsync(leaseId, cancellationToken);

        // Filter charges applicable for this billing period
        var applicableCharges = recurringCharges
            .Where(charge => charge.Frequency == BillingFrequency.Monthly
                && charge.StartDate <= billingPeriodEnd
                && (!charge.EndDate.HasValue || charge.EndDate >= billingPeriodStart));

        var lineItems = new List<RecurringChargeLineItem>();
        decimal totalAmount = 0;

        foreach (var charge in applicableCharges)
        {

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
                ChargeTypeId = charge.ChargeTypeId,
                ChargeDescription = charge.Description,
                PeriodStart = effectiveStart,
                PeriodEnd = effectiveEnd,
                FullAmount = charge.Amount,
                Amount = amount,
                IsProrated = isProrated,
                Frequency = charge.Frequency
            };

            lineItems.Add(lineItem);
            totalAmount += amount;
        }

        return new RecurringChargeCalculationResult
        {
            TotalAmount = totalAmount,
            LineItems = lineItems
        };
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

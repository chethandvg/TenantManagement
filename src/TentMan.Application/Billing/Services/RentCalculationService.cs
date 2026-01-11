using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Service for calculating rent charges with support for multiple terms and proration.
/// </summary>
public class RentCalculationService : IRentCalculationService
{
    private readonly ILeaseRepository _leaseRepository;
    private readonly ActualDaysInMonthCalculator _actualDaysCalculator;
    private readonly ThirtyDayMonthCalculator _thirtyDayCalculator;

    public RentCalculationService(ILeaseRepository leaseRepository)
    {
        _leaseRepository = leaseRepository;
        _actualDaysCalculator = new ActualDaysInMonthCalculator();
        _thirtyDayCalculator = new ThirtyDayMonthCalculator();
    }

    /// <inheritdoc/>
    public async Task<RentCalculationResult> CalculateRentAsync(
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default)
    {
        if (billingPeriodEnd < billingPeriodStart)
            throw new ArgumentException("Billing period end cannot be before start", nameof(billingPeriodEnd));

        var lease = await _leaseRepository.GetByIdWithDetailsAsync(leaseId, cancellationToken);
        if (lease == null)
            throw new InvalidOperationException($"Lease with ID {leaseId} not found");

        var calculator = GetCalculator(prorationMethod);

        // Get applicable terms for the billing period
        var applicableTerms = lease.Terms
            .Where(t => t.EffectiveFrom <= billingPeriodEnd && 
                       (t.EffectiveTo == null || t.EffectiveTo >= billingPeriodStart))
            .OrderBy(t => t.EffectiveFrom)
            .ToList();

        if (!applicableTerms.Any())
        {
            return new RentCalculationResult(); // No terms applicable for this period
        }

        var lineItems = new List<RentLineItem>();
        decimal totalAmount = 0;

        foreach (var term in applicableTerms)
        {
            var termStart = term.EffectiveFrom > billingPeriodStart ? term.EffectiveFrom : billingPeriodStart;
            var termEnd = term.EffectiveTo.HasValue && term.EffectiveTo < billingPeriodEnd 
                ? term.EffectiveTo.Value 
                : billingPeriodEnd;

            if (termEnd < termStart)
                continue; // Skip invalid period

            var isProrated = termStart != billingPeriodStart || termEnd != billingPeriodEnd;
            var amount = calculator.CalculateProration(
                term.MonthlyRent,
                termStart,
                termEnd,
                billingPeriodStart,
                billingPeriodEnd);

            var lineItem = new RentLineItem
            {
                LeaseTermId = term.Id,
                PeriodStart = termStart,
                PeriodEnd = termEnd,
                FullMonthlyRent = term.MonthlyRent,
                Amount = amount,
                IsProrated = isProrated,
                Description = isProrated 
                    ? $"Rent for {termStart:MMM dd} - {termEnd:MMM dd, yyyy} (Prorated)"
                    : $"Rent for {termStart:MMM yyyy}"
            };

            lineItems.Add(lineItem);
            totalAmount += amount;
        }

        return new RentCalculationResult 
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

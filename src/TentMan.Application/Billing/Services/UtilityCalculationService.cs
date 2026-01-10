using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Service for calculating utility charges with support for multiple calculation methods.
/// </summary>
public class UtilityCalculationService : IUtilityCalculationService
{
    private readonly IUtilityRatePlanRepository _ratePlanRepository;

    public UtilityCalculationService(IUtilityRatePlanRepository ratePlanRepository)
    {
        _ratePlanRepository = ratePlanRepository;
    }

    /// <inheritdoc/>
    public UtilityCalculationResult CalculateAmountBased(
        decimal amount,
        UtilityType utilityType)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        return new UtilityCalculationResult
        {
            UtilityType = utilityType,
            IsMeterBased = false,
            TotalAmount = amount,
            Description = $"{utilityType} - Direct billing"
        };
    }

    /// <inheritdoc/>
    public UtilityCalculationResult CalculateMeterBasedFlatRate(
        decimal unitsConsumed,
        decimal ratePerUnit,
        decimal fixedCharge,
        UtilityType utilityType)
    {
        if (unitsConsumed < 0)
            throw new ArgumentException("Units consumed cannot be negative", nameof(unitsConsumed));
        
        if (ratePerUnit < 0)
            throw new ArgumentException("Rate per unit cannot be negative", nameof(ratePerUnit));
        
        if (fixedCharge < 0)
            throw new ArgumentException("Fixed charge cannot be negative", nameof(fixedCharge));

        var consumptionCharge = unitsConsumed * ratePerUnit;
        var totalAmount = consumptionCharge + fixedCharge;

        var result = new UtilityCalculationResult
        {
            UtilityType = utilityType,
            IsMeterBased = true,
            UnitsConsumed = unitsConsumed,
            TotalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero),
            Description = $"{utilityType} - {unitsConsumed} units @ {ratePerUnit:C}/unit"
        };

        // Add breakdown
        result.SlabBreakdown.Add(new UtilitySlabLineItem
        {
            FromUnits = 0,
            ToUnits = unitsConsumed,
            UnitsInSlab = unitsConsumed,
            RatePerUnit = ratePerUnit,
            Amount = Math.Round(consumptionCharge, 2, MidpointRounding.AwayFromZero),
            FixedCharge = fixedCharge > 0 ? fixedCharge : null
        });

        return result;
    }

    /// <inheritdoc/>
    public async Task<UtilityCalculationResult> CalculateMeterBasedSlabsAsync(
        decimal unitsConsumed,
        Guid ratePlanId,
        UtilityType utilityType,
        CancellationToken cancellationToken = default)
    {
        if (unitsConsumed < 0)
            throw new ArgumentException("Units consumed cannot be negative", nameof(unitsConsumed));

        var ratePlan = await _ratePlanRepository.GetByIdWithSlabsAsync(ratePlanId, cancellationToken);
        if (ratePlan == null)
            throw new InvalidOperationException($"Utility rate plan with ID {ratePlanId} not found");

        if (!ratePlan.IsActive)
            throw new InvalidOperationException($"Utility rate plan {ratePlanId} is not active");

        var slabs = ratePlan.RateSlabs.OrderBy(s => s.SlabOrder).ToList();
        if (!slabs.Any())
            throw new InvalidOperationException($"Utility rate plan {ratePlanId} has no rate slabs defined");

        var result = new UtilityCalculationResult
        {
            UtilityType = utilityType,
            IsMeterBased = true,
            UnitsConsumed = unitsConsumed,
            Description = $"{utilityType} - {unitsConsumed} units (Slab-based)"
        };

        decimal remainingUnits = unitsConsumed;
        decimal totalAmount = 0;

        foreach (var slab in slabs)
        {
            if (remainingUnits <= 0)
                break;

            // Calculate units in this slab
            var slabFromUnits = slab.FromUnits;
            var slabToUnits = slab.ToUnits ?? decimal.MaxValue;
            
            // Determine how many units fall in this slab
            var unitsInSlab = Math.Min(remainingUnits, slabToUnits - slabFromUnits);
            
            if (unitsInSlab <= 0)
                continue;

            var slabAmount = unitsInSlab * slab.RatePerUnit;
            var slabFixedCharge = slab.FixedCharge ?? 0;
            totalAmount += slabAmount + slabFixedCharge;

            result.SlabBreakdown.Add(new UtilitySlabLineItem
            {
                FromUnits = slabFromUnits,
                ToUnits = slabToUnits == decimal.MaxValue ? unitsConsumed : slabToUnits,
                UnitsInSlab = unitsInSlab,
                RatePerUnit = slab.RatePerUnit,
                Amount = Math.Round(slabAmount, 2, MidpointRounding.AwayFromZero),
                FixedCharge = slabFixedCharge > 0 ? slabFixedCharge : null
            });

            remainingUnits -= unitsInSlab;
        }

        result.TotalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
        return result;
    }
}

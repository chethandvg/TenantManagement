using TentMan.Application.Billing.Services;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class UtilityCalculationServiceTests
{
    private readonly Mock<IUtilityRatePlanRepository> _mockRepository;
    private readonly UtilityCalculationService _service;

    public UtilityCalculationServiceTests()
    {
        _mockRepository = new Mock<IUtilityRatePlanRepository>();
        _service = new UtilityCalculationService(_mockRepository.Object);
    }

    #region Amount-Based Tests

    [Fact]
    public void CalculateAmountBased_ValidAmount_ReturnsCorrectResult()
    {
        // Arrange
        var amount = 1500m;
        var utilityType = UtilityType.Electricity;

        // Act
        var result = _service.CalculateAmountBased(amount, utilityType);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(amount);
        result.UtilityType.Should().Be(utilityType);
        result.IsMeterBased.Should().BeFalse();
        result.UnitsConsumed.Should().BeNull();
        result.Description.Should().Contain("Direct billing");
    }

    [Fact]
    public void CalculateAmountBased_ZeroAmount_ReturnsZero()
    {
        // Arrange
        var amount = 0m;
        var utilityType = UtilityType.Water;

        // Act
        var result = _service.CalculateAmountBased(amount, utilityType);

        // Assert
        result.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void CalculateAmountBased_NegativeAmount_ThrowsException()
    {
        // Arrange
        var amount = -100m;

        // Act
        var act = () => _service.CalculateAmountBased(amount, UtilityType.Gas);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount cannot be negative*");
    }

    #endregion

    #region Meter-Based Flat Rate Tests

    [Fact]
    public void CalculateMeterBasedFlatRate_ValidInputs_ReturnsCorrectResult()
    {
        // Arrange
        var unitsConsumed = 100m;
        var ratePerUnit = 5.50m;
        var fixedCharge = 50m;
        var utilityType = UtilityType.Electricity;

        // Act
        var result = _service.CalculateMeterBasedFlatRate(
            unitsConsumed, ratePerUnit, fixedCharge, utilityType);

        // Assert
        result.Should().NotBeNull();
        result.IsMeterBased.Should().BeTrue();
        result.UnitsConsumed.Should().Be(unitsConsumed);
        result.TotalAmount.Should().Be(600m); // 100 * 5.50 + 50 = 600
        result.UtilityType.Should().Be(utilityType);
        result.SlabBreakdown.Should().HaveCount(1);
        result.SlabBreakdown[0].Amount.Should().Be(550m);
        result.SlabBreakdown[0].FixedCharge.Should().Be(50m);
    }

    [Fact]
    public void CalculateMeterBasedFlatRate_NoFixedCharge_CalculatesCorrectly()
    {
        // Arrange
        var unitsConsumed = 200m;
        var ratePerUnit = 3m;
        var fixedCharge = 0m;

        // Act
        var result = _service.CalculateMeterBasedFlatRate(
            unitsConsumed, ratePerUnit, fixedCharge, UtilityType.Water);

        // Assert
        result.TotalAmount.Should().Be(600m); // 200 * 3 = 600
        result.SlabBreakdown[0].FixedCharge.Should().BeNull();
    }

    [Fact]
    public void CalculateMeterBasedFlatRate_RoundsToTwoDecimals()
    {
        // Arrange
        var unitsConsumed = 333m;
        var ratePerUnit = 0.33m;
        var fixedCharge = 10.55m;

        // Act
        var result = _service.CalculateMeterBasedFlatRate(
            unitsConsumed, ratePerUnit, fixedCharge, UtilityType.Gas);

        // Assert
        // 333 * 0.33 = 109.89, + 10.55 = 120.44
        result.TotalAmount.Should().Be(120.44m);
        result.SlabBreakdown[0].Amount.Should().Be(109.89m);
    }

    [Fact]
    public void CalculateMeterBasedFlatRate_NegativeUnits_ThrowsException()
    {
        // Act
        var act = () => _service.CalculateMeterBasedFlatRate(
            -10m, 5m, 0m, UtilityType.Electricity);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Units consumed cannot be negative*");
    }

    [Fact]
    public void CalculateMeterBasedFlatRate_NegativeRate_ThrowsException()
    {
        // Act
        var act = () => _service.CalculateMeterBasedFlatRate(
            100m, -5m, 0m, UtilityType.Water);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Rate per unit cannot be negative*");
    }

    [Fact]
    public void CalculateMeterBasedFlatRate_NegativeFixedCharge_ThrowsException()
    {
        // Act
        var act = () => _service.CalculateMeterBasedFlatRate(
            100m, 5m, -10m, UtilityType.Gas);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Fixed charge cannot be negative*");
    }

    #endregion

    #region Meter-Based Slabs Tests

    [Fact]
    public async Task CalculateMeterBasedSlabs_SingleSlab_CalculatesCorrectly()
    {
        // Arrange
        var ratePlanId = Guid.NewGuid();
        var unitsConsumed = 50m;
        var ratePlan = CreateRatePlan(ratePlanId, UtilityType.Electricity, new[]
        {
            new { From = 0m, To = (decimal?)100m, Rate = 5m, Fixed = (decimal?)0m }
        });

        _mockRepository
            .Setup(r => r.GetByIdWithSlabsAsync(ratePlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratePlan);

        // Act
        var result = await _service.CalculateMeterBasedSlabsAsync(
            unitsConsumed, ratePlanId, UtilityType.Electricity);

        // Assert
        result.TotalAmount.Should().Be(250m); // 50 * 5 = 250
        result.IsMeterBased.Should().BeTrue();
        result.UnitsConsumed.Should().Be(unitsConsumed);
        result.SlabBreakdown.Should().HaveCount(1);
        result.SlabBreakdown[0].UnitsInSlab.Should().Be(50m);
    }

    [Fact]
    public async Task CalculateMeterBasedSlabs_MultipleSlabs_CalculatesCorrectly()
    {
        // Arrange
        var ratePlanId = Guid.NewGuid();
        var unitsConsumed = 250m;
        var ratePlan = CreateRatePlan(ratePlanId, UtilityType.Electricity, new[]
        {
            new { From = 0m, To = (decimal?)100m, Rate = 3m, Fixed = (decimal?)0m },
            new { From = 100m, To = (decimal?)200m, Rate = 4m, Fixed = (decimal?)0m },
            new { From = 200m, To = (decimal?)null, Rate = 5m, Fixed = (decimal?)0m }
        });

        _mockRepository
            .Setup(r => r.GetByIdWithSlabsAsync(ratePlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratePlan);

        // Act
        var result = await _service.CalculateMeterBasedSlabsAsync(
            unitsConsumed, ratePlanId, UtilityType.Electricity);

        // Assert
        // Slab 1: 100 * 3 = 300
        // Slab 2: 100 * 4 = 400
        // Slab 3: 50 * 5 = 250
        // Total: 950
        result.TotalAmount.Should().Be(950m);
        result.SlabBreakdown.Should().HaveCount(3);
        result.SlabBreakdown[0].UnitsInSlab.Should().Be(100m);
        result.SlabBreakdown[1].UnitsInSlab.Should().Be(100m);
        result.SlabBreakdown[2].UnitsInSlab.Should().Be(50m);
    }

    [Fact]
    public async Task CalculateMeterBasedSlabs_WithFixedCharges_IncludesInTotal()
    {
        // Arrange
        var ratePlanId = Guid.NewGuid();
        var unitsConsumed = 150m;
        var ratePlan = CreateRatePlan(ratePlanId, UtilityType.Water, new[]
        {
            new { From = 0m, To = (decimal?)100m, Rate = 2m, Fixed = (decimal?)50m },
            new { From = 100m, To = (decimal?)null, Rate = 3m, Fixed = (decimal?)25m }
        });

        _mockRepository
            .Setup(r => r.GetByIdWithSlabsAsync(ratePlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratePlan);

        // Act
        var result = await _service.CalculateMeterBasedSlabsAsync(
            unitsConsumed, ratePlanId, UtilityType.Water);

        // Assert
        // Slab 1: (100 * 2) + 50 = 250
        // Slab 2: (50 * 3) + 25 = 175
        // Total: 425
        result.TotalAmount.Should().Be(425m);
        result.SlabBreakdown[0].FixedCharge.Should().Be(50m);
        result.SlabBreakdown[1].FixedCharge.Should().Be(25m);
    }

    [Fact]
    public async Task CalculateMeterBasedSlabs_RatePlanNotFound_ThrowsException()
    {
        // Arrange
        var ratePlanId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdWithSlabsAsync(ratePlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilityRatePlan?)null);

        // Act
        var act = async () => await _service.CalculateMeterBasedSlabsAsync(
            100m, ratePlanId, UtilityType.Electricity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{ratePlanId}*not found*");
    }

    [Fact]
    public async Task CalculateMeterBasedSlabs_InactiveRatePlan_ThrowsException()
    {
        // Arrange
        var ratePlanId = Guid.NewGuid();
        var ratePlan = CreateRatePlan(ratePlanId, UtilityType.Electricity, new[]
        {
            new { From = 0m, To = (decimal?)100m, Rate = 5m, Fixed = (decimal?)0m }
        });
        ratePlan.IsActive = false;

        _mockRepository
            .Setup(r => r.GetByIdWithSlabsAsync(ratePlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratePlan);

        // Act
        var act = async () => await _service.CalculateMeterBasedSlabsAsync(
            100m, ratePlanId, UtilityType.Electricity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task CalculateMeterBasedSlabs_NoSlabs_ThrowsException()
    {
        // Arrange
        var ratePlanId = Guid.NewGuid();
        var ratePlan = new UtilityRatePlan
        {
            Id = ratePlanId,
            UtilityType = UtilityType.Electricity,
            IsActive = true,
            RateSlabs = new List<UtilityRateSlab>()
        };

        _mockRepository
            .Setup(r => r.GetByIdWithSlabsAsync(ratePlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ratePlan);

        // Act
        var act = async () => await _service.CalculateMeterBasedSlabsAsync(
            100m, ratePlanId, UtilityType.Electricity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no rate slabs defined*");
    }

    [Fact]
    public async Task CalculateMeterBasedSlabs_NegativeUnits_ThrowsException()
    {
        // Arrange
        var ratePlanId = Guid.NewGuid();

        // Act
        var act = async () => await _service.CalculateMeterBasedSlabsAsync(
            -10m, ratePlanId, UtilityType.Electricity);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Units consumed cannot be negative*");
    }

    #endregion

    #region Helper Methods

    private static UtilityRatePlan CreateRatePlan(
        Guid id,
        UtilityType utilityType,
        dynamic[] slabDefinitions)
    {
        var ratePlan = new UtilityRatePlan
        {
            Id = id,
            UtilityType = utilityType,
            IsActive = true,
            RateSlabs = new List<UtilityRateSlab>()
        };

        for (int i = 0; i < slabDefinitions.Length; i++)
        {
            var slabDef = slabDefinitions[i];
            ratePlan.RateSlabs.Add(new UtilityRateSlab
            {
                Id = Guid.NewGuid(),
                UtilityRatePlanId = id,
                SlabOrder = i + 1,
                FromUnits = slabDef.From,
                ToUnits = slabDef.To,
                RatePerUnit = slabDef.Rate,
                FixedCharge = slabDef.Fixed
            });
        }

        return ratePlan;
    }

    #endregion
}

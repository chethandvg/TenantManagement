using TentMan.Application.Billing.Services;
using FluentAssertions;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class ActualDaysInMonthCalculatorTests
{
    private readonly ActualDaysInMonthCalculator _calculator;

    public ActualDaysInMonthCalculatorTests()
    {
        _calculator = new ActualDaysInMonthCalculator();
    }

    #region Happy Path Tests

    [Fact]
    public void CalculateProration_FullMonthPeriod_ReturnsFullAmount()
    {
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            billingStart,
            billingEnd,
            billingStart,
            billingEnd);

        // Assert
        result.Should().Be(fullAmount);
    }

    [Fact]
    public void CalculateProration_HalfMonthPeriod_ReturnsHalfAmount()
    {
        // Arrange - January has 31 days, so 15 days should be approximately half
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2024, 1, 1);
        var usageEnd = new DateOnly(2024, 1, 15);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        // 15 days out of 31 = 15/31 * 10000 = 4838.71
        result.Should().Be(4838.71m);
    }

    [Fact]
    public void CalculateProration_PartialMonthAtStart_CalculatesCorrectly()
    {
        // Arrange - Started on Jan 15, billed through Jan 31
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2024, 1, 15);
        var usageEnd = new DateOnly(2024, 1, 31);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        // 17 days (15-31 inclusive) out of 31 = 17/31 * 10000 = 5483.87
        result.Should().Be(5483.87m);
    }

    [Fact]
    public void CalculateProration_PartialMonthAtEnd_CalculatesCorrectly()
    {
        // Arrange - Ended on Jan 15, billed from Jan 1
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2024, 1, 1);
        var usageEnd = new DateOnly(2024, 1, 15);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        // 15 days out of 31 = 15/31 * 10000 = 4838.71
        result.Should().Be(4838.71m);
    }

    [Fact]
    public void CalculateProration_LeapYearFebruary_UsesActualDays()
    {
        // Arrange - February 2024 has 29 days (leap year)
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            billingStart,
            billingEnd,
            billingStart,
            billingEnd);

        // Assert
        result.Should().Be(fullAmount);
    }

    [Fact]
    public void CalculateProration_NonLeapYearFebruary_UsesActualDays()
    {
        // Arrange - February 2023 has 28 days (non-leap year)
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2023, 2, 1);
        var billingEnd = new DateOnly(2023, 2, 28);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            billingStart,
            billingEnd,
            billingStart,
            billingEnd);

        // Assert
        result.Should().Be(fullAmount);
    }

    [Fact]
    public void CalculateProration_SingleDay_CalculatesCorrectly()
    {
        // Arrange
        var fullAmount = 3100m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var singleDay = new DateOnly(2024, 1, 15);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            singleDay,
            singleDay,
            billingStart,
            billingEnd);

        // Assert
        // 1 day out of 31 = 1/31 * 3100 = 100
        result.Should().Be(100m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateProration_UsagePeriodOutsideBillingPeriod_ReturnsZero()
    {
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2024, 2, 1);
        var usageEnd = new DateOnly(2024, 2, 15);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateProration_UsagePeriodPartiallyOverlaps_CalculatesOverlapOnly()
    {
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2023, 12, 15);
        var usageEnd = new DateOnly(2024, 1, 15);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        // Overlap is Jan 1-15 = 15 days out of 31 = 15/31 * 10000 = 4838.71
        result.Should().Be(4838.71m);
    }

    [Fact]
    public void CalculateProration_ZeroAmount_ReturnsZero()
    {
        // Arrange
        var fullAmount = 0m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            billingStart,
            billingEnd,
            billingStart,
            billingEnd);

        // Assert
        result.Should().Be(0m);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void CalculateProration_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var fullAmount = -100m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        // Act
        var act = () => _calculator.CalculateProration(
            fullAmount,
            billingStart,
            billingEnd,
            billingStart,
            billingEnd);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Full amount cannot be negative*");
    }

    [Fact]
    public void CalculateProration_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2024, 1, 20);
        var usageEnd = new DateOnly(2024, 1, 10);

        // Act
        var act = () => _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*End date cannot be before start date*");
    }

    [Fact]
    public void CalculateProration_BillingEndBeforeStart_ThrowsArgumentException()
    {
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 31);
        var billingEnd = new DateOnly(2024, 1, 1);

        // Act
        var act = () => _calculator.CalculateProration(
            fullAmount,
            billingStart,
            billingEnd,
            billingStart,
            billingEnd);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*End date cannot be before start date*");
    }

    #endregion

    #region Rounding Tests

    [Fact]
    public void CalculateProration_RoundsToTwoDecimalPlaces()
    {
        // Arrange - This will produce a result that needs rounding
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2024, 1, 1);
        var usageEnd = new DateOnly(2024, 1, 10);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        // 10 days out of 31 = 10/31 * 10000 = 3225.806451... should round to 3225.81
        result.Should().Be(3225.81m);
        // Verify it's rounded to 2 decimal places
        result.Should().Be(Math.Round(result, 2));
    }

    #endregion
}

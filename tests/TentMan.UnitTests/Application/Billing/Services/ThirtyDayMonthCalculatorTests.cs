using TentMan.Application.Billing.Services;
using FluentAssertions;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class ThirtyDayMonthCalculatorTests
{
    private readonly ThirtyDayMonthCalculator _calculator;

    public ThirtyDayMonthCalculatorTests()
    {
        _calculator = new ThirtyDayMonthCalculator();
    }

    #region Happy Path Tests

    [Fact]
    public void CalculateProration_ThirtyDays_ReturnsFullAmount()
    {
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);

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
    public void CalculateProration_FifteenDays_ReturnsHalfAmount()
    {
        // Arrange - 15 days should be exactly half in 30-day month
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);
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
        // 15 days / 30 days * 10000 = 5000
        result.Should().Be(5000m);
    }

    [Fact]
    public void CalculateProration_ThirtyOneDayMonth_StillUses30DayBase()
    {
        // Arrange - January has 31 days, but calculator uses 30
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
        // 15 actual days / 30 (not 31) * 10000 = 5000
        result.Should().Be(5000m);
    }

    [Fact]
    public void CalculateProration_February28Days_StillUses30DayBase()
    {
        // Arrange - February 2023 has 28 days, but calculator uses 30
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2023, 2, 1);
        var billingEnd = new DateOnly(2023, 2, 28);
        var usageStart = new DateOnly(2023, 2, 1);
        var usageEnd = new DateOnly(2023, 2, 14);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        // 14 actual days / 30 * 10000 = 4666.67
        result.Should().Be(4666.67m);
    }

    [Fact]
    public void CalculateProration_LeapYearFebruary_StillUses30DayBase()
    {
        // Arrange - February 2024 has 29 days, but calculator uses 30
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);
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
        // 15 actual days / 30 * 10000 = 5000
        result.Should().Be(5000m);
    }

    [Fact]
    public void CalculateProration_SingleDay_CalculatesCorrectly()
    {
        // Arrange
        var fullAmount = 3000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);
        var singleDay = new DateOnly(2024, 1, 15);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            singleDay,
            singleDay,
            billingStart,
            billingEnd);

        // Assert
        // 1 day / 30 days * 3000 = 100
        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateProration_TenDays_CalculatesCorrectly()
    {
        // Arrange
        var fullAmount = 9000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);
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
        // 10 days / 30 * 9000 = 3000
        result.Should().Be(3000m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CalculateProration_UsagePeriodOutsideBillingPeriod_ReturnsZero()
    {
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);
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
        var billingEnd = new DateOnly(2024, 1, 30);
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
        // Overlap is Jan 1-15 = 15 days / 30 * 10000 = 5000
        result.Should().Be(5000m);
    }

    [Fact]
    public void CalculateProration_ZeroAmount_ReturnsZero()
    {
        // Arrange
        var fullAmount = 0m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);

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

    [Fact]
    public void CalculateProration_FullThirtyOneDayMonth_ExceedsBaseBy1Day()
    {
        // Arrange - Full 31-day month
        var fullAmount = 3100m;
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
        // 31 days / 30 * 3100 = 3203.33 (slightly over full amount due to 31 days)
        result.Should().Be(3203.33m);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void CalculateProration_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var fullAmount = -100m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);

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
        var billingEnd = new DateOnly(2024, 1, 30);
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
        var billingStart = new DateOnly(2024, 1, 30);
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
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 30);
        var usageStart = new DateOnly(2024, 1, 1);
        var usageEnd = new DateOnly(2024, 1, 7);

        // Act
        var result = _calculator.CalculateProration(
            fullAmount,
            usageStart,
            usageEnd,
            billingStart,
            billingEnd);

        // Assert
        // 7 days / 30 * 10000 = 2333.333... should round to 2333.33
        result.Should().Be(2333.33m);
        // Verify it's rounded to 2 decimal places
        result.Should().Be(Math.Round(result, 2));
    }

    #endregion

    #region Comparison with Actual Days Method

    [Fact]
    public void CalculateProration_DifferentFromActualDays_In31DayMonth()
    {
        // This test demonstrates the difference between the two methods
        // Arrange
        var fullAmount = 10000m;
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var usageStart = new DateOnly(2024, 1, 1);
        var usageEnd = new DateOnly(2024, 1, 15);

        var actualDaysCalculator = new ActualDaysInMonthCalculator();

        // Act
        var thirtyDayResult = _calculator.CalculateProration(
            fullAmount, usageStart, usageEnd, billingStart, billingEnd);
        
        var actualDaysResult = actualDaysCalculator.CalculateProration(
            fullAmount, usageStart, usageEnd, billingStart, billingEnd);

        // Assert
        // 30-day method: 15/30 * 10000 = 5000
        thirtyDayResult.Should().Be(5000m);
        
        // Actual days method: 15/31 * 10000 = 4838.71
        actualDaysResult.Should().Be(4838.71m);
        
        // They should be different
        thirtyDayResult.Should().NotBe(actualDaysResult);
    }

    #endregion
}

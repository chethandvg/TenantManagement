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
public class RecurringChargeCalculationServiceTests
{
    private readonly Mock<ILeaseRecurringChargeRepository> _mockRepository;
    private readonly RecurringChargeCalculationService _service;

    public RecurringChargeCalculationServiceTests()
    {
        _mockRepository = new Mock<ILeaseRecurringChargeRepository>();
        _service = new RecurringChargeCalculationService(_mockRepository.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task CalculateChargesAsync_SingleCharge_FullPeriod_ReturnsFullAmount()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var chargeAmount = 500m;

        var charges = new List<LeaseRecurringCharge>
        {
            CreateCharge(leaseId, "Parking", chargeAmount, billingStart, null)
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(chargeAmount);
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].IsProrated.Should().BeFalse();
        result.LineItems[0].Amount.Should().Be(chargeAmount);
    }

    [Fact]
    public async Task CalculateChargesAsync_ChargeStartsMidPeriod_ReturnsProrated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var chargeAmount = 1000m;

        var charges = new List<LeaseRecurringCharge>
        {
            CreateCharge(leaseId, "Maintenance", chargeAmount, new DateOnly(2024, 1, 15), null)
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].IsProrated.Should().BeTrue();
        result.LineItems[0].PeriodStart.Should().Be(new DateOnly(2024, 1, 15));
        // 17 days out of 31 = 17/31 * 1000 = 548.39
        result.TotalAmount.Should().Be(548.39m);
    }

    [Fact]
    public async Task CalculateChargesAsync_ChargeEndsMidPeriod_ReturnsProrated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var chargeAmount = 600m;

        var charges = new List<LeaseRecurringCharge>
        {
            CreateCharge(leaseId, "Storage", chargeAmount, billingStart, new DateOnly(2024, 1, 15))
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].IsProrated.Should().BeTrue();
        result.LineItems[0].PeriodEnd.Should().Be(new DateOnly(2024, 1, 15));
        // 15 days out of 31 = 15/31 * 600 = 290.32
        result.TotalAmount.Should().Be(290.32m);
    }

    [Fact]
    public async Task CalculateChargesAsync_ChargeStartsAndEndsMidPeriod_ReturnsProrated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var chargeAmount = 900m;

        var charges = new List<LeaseRecurringCharge>
        {
            CreateCharge(leaseId, "Temporary Parking", chargeAmount, 
                new DateOnly(2024, 1, 10), new DateOnly(2024, 1, 20))
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].IsProrated.Should().BeTrue();
        result.LineItems[0].PeriodStart.Should().Be(new DateOnly(2024, 1, 10));
        result.LineItems[0].PeriodEnd.Should().Be(new DateOnly(2024, 1, 20));
        // 11 days out of 31 = 11/31 * 900 = 319.35
        result.TotalAmount.Should().Be(319.35m);
    }

    [Fact]
    public async Task CalculateChargesAsync_MultipleCharges_ReturnsAllCharges()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var charges = new List<LeaseRecurringCharge>
        {
            CreateCharge(leaseId, "Parking", 500m, billingStart, null),
            CreateCharge(leaseId, "Maintenance", 300m, billingStart, null),
            CreateCharge(leaseId, "Storage", 200m, billingStart, null)
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().HaveCount(3);
        result.TotalAmount.Should().Be(1000m); // 500 + 300 + 200
    }

    [Fact]
    public async Task CalculateChargesAsync_ThirtyDayMethod_CalculatesDifferently()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var chargeAmount = 900m;

        var charges = new List<LeaseRecurringCharge>
        {
            CreateCharge(leaseId, "Parking", chargeAmount, new DateOnly(2024, 1, 15), null)
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ThirtyDayMonth);

        // Assert
        // 17 actual days / 30 * 900 = 510
        result.TotalAmount.Should().Be(510m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CalculateChargesAsync_NoActiveCharges_ReturnsEmptyResult()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LeaseRecurringCharge>());

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(0m);
        result.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateChargesAsync_ChargeOutsideBillingPeriod_Excluded()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);

        var charges = new List<LeaseRecurringCharge>
        {
            // This charge ended before the billing period
            CreateCharge(leaseId, "Old Charge", 500m, 
                new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31))
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().BeEmpty();
        result.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateChargesAsync_ChargeStartsAfterBillingPeriod_Excluded()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var charges = new List<LeaseRecurringCharge>
        {
            // This charge starts after the billing period
            CreateCharge(leaseId, "Future Charge", 500m, 
                new DateOnly(2024, 2, 1), null)
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().BeEmpty();
        result.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateChargesAsync_OnlyMonthlyFrequency_OthersExcluded()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var charges = new List<LeaseRecurringCharge>
        {
            CreateCharge(leaseId, "Monthly", 500m, billingStart, null, BillingFrequency.Monthly),
            CreateCharge(leaseId, "Quarterly", 1500m, billingStart, null, BillingFrequency.Quarterly),
            CreateCharge(leaseId, "Yearly", 6000m, billingStart, null, BillingFrequency.Yearly)
        };

        _mockRepository
            .Setup(r => r.GetActiveByLeaseIdAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(charges);

        // Act
        var result = await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].ChargeDescription.Should().Be("Monthly");
        result.TotalAmount.Should().Be(500m);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CalculateChargesAsync_InvalidBillingPeriod_ThrowsException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 31);
        var billingEnd = new DateOnly(2024, 1, 1);

        // Act
        var act = async () => await _service.CalculateChargesAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Billing period end cannot be before start*");
    }

    #endregion

    #region Helper Methods

    private static LeaseRecurringCharge CreateCharge(
        Guid leaseId,
        string description,
        decimal amount,
        DateOnly startDate,
        DateOnly? endDate,
        BillingFrequency frequency = BillingFrequency.Monthly)
    {
        return new LeaseRecurringCharge
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            Description = description,
            Amount = amount,
            StartDate = startDate,
            EndDate = endDate,
            Frequency = frequency,
            IsActive = true
        };
    }

    #endregion
}

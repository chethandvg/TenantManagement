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
public class RentCalculationServiceTests
{
    private readonly Mock<ILeaseRepository> _mockLeaseRepository;
    private readonly RentCalculationService _service;

    public RentCalculationServiceTests()
    {
        _mockLeaseRepository = new Mock<ILeaseRepository>();
        _service = new RentCalculationService(_mockLeaseRepository.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task CalculateRentAsync_SingleTerm_FullPeriod_ReturnsFullAmount()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var monthlyRent = 10000m;

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2024, 1, 1), To = (DateOnly?)null, Rent = monthlyRent }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(monthlyRent);
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].IsProrated.Should().BeFalse();
        result.LineItems[0].Amount.Should().Be(monthlyRent);
    }

    [Fact]
    public async Task CalculateRentAsync_SingleTerm_PartialPeriod_ReturnsProrated()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var monthlyRent = 10000m;

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2024, 1, 15), To = (DateOnly?)null, Rent = monthlyRent }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].IsProrated.Should().BeTrue();
        result.LineItems[0].PeriodStart.Should().Be(new DateOnly(2024, 1, 15));
        result.LineItems[0].PeriodEnd.Should().Be(billingEnd);
        // 17 days out of 31 = 17/31 * 10000 = 5483.87
        result.TotalAmount.Should().Be(5483.87m);
    }

    [Fact]
    public async Task CalculateRentAsync_MultipleTerms_MidPeriodChange_ReturnsMultipleLines()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2024, 1, 1), To = (DateOnly?)new DateOnly(2024, 1, 15), Rent = 10000m },
            new { From = new DateOnly(2024, 1, 16), To = (DateOnly?)null, Rent = 12000m }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.LineItems.Should().HaveCount(2);
        
        // First term: Jan 1-15 = 15 days of 31 = 15/31 * 10000 = 4838.71
        result.LineItems[0].PeriodStart.Should().Be(new DateOnly(2024, 1, 1));
        result.LineItems[0].PeriodEnd.Should().Be(new DateOnly(2024, 1, 15));
        result.LineItems[0].Amount.Should().Be(4838.71m);
        result.LineItems[0].IsProrated.Should().BeTrue();
        
        // Second term: Jan 16-31 = 16 days of 31 = 16/31 * 12000 = 6193.55
        result.LineItems[1].PeriodStart.Should().Be(new DateOnly(2024, 1, 16));
        result.LineItems[1].PeriodEnd.Should().Be(new DateOnly(2024, 1, 31));
        result.LineItems[1].Amount.Should().Be(6193.55m);
        result.LineItems[1].IsProrated.Should().BeTrue();
        
        // Total
        result.TotalAmount.Should().Be(11032.26m);
    }

    [Fact]
    public async Task CalculateRentAsync_ThirtyDayMethod_CalculatesDifferently()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);
        var monthlyRent = 10000m;

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2024, 1, 15), To = (DateOnly?)null, Rent = monthlyRent }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ThirtyDayMonth);

        // Assert
        // 17 actual days / 30 * 10000 = 5666.67
        result.TotalAmount.Should().Be(5666.67m);
    }

    [Fact]
    public async Task CalculateRentAsync_NoApplicableTerms_ReturnsEmptyResult()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2024, 1, 1), To = (DateOnly?)new DateOnly(2024, 1, 31), Rent = 10000m }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(0m);
        result.LineItems.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateRentAsync_TermOutsideBillingPeriod_ExcludesTerm()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2023, 12, 1), To = (DateOnly?)new DateOnly(2023, 12, 31), Rent = 8000m },
            new { From = new DateOnly(2024, 1, 1), To = (DateOnly?)null, Rent = 10000m }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].FullMonthlyRent.Should().Be(10000m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CalculateRentAsync_TermEndsBeforeBillingEnd_ProratesCorrectly()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2024, 1, 1), To = (DateOnly?)new DateOnly(2024, 1, 20), Rent = 10000m }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].PeriodEnd.Should().Be(new DateOnly(2024, 1, 20));
        // 20 days of 31 = 20/31 * 10000 = 6451.61
        result.TotalAmount.Should().Be(6451.61m);
    }

    [Fact]
    public async Task CalculateRentAsync_LeapYearFebruary_UsesCorrectDays()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);
        var monthlyRent = 10000m;

        var lease = CreateLeaseWithTerms(leaseId, new[]
        {
            new { From = new DateOnly(2024, 2, 1), To = (DateOnly?)null, Rent = monthlyRent }
        });

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        // Act
        var result = await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.TotalAmount.Should().Be(monthlyRent);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CalculateRentAsync_InvalidBillingPeriod_ThrowsException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 31);
        var billingEnd = new DateOnly(2024, 1, 1);

        // Act
        var act = async () => await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Billing period end cannot be before start*");
    }

    [Fact]
    public async Task CalculateRentAsync_LeaseNotFound_ThrowsException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        _mockLeaseRepository
            .Setup(r => r.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lease?)null);

        // Act
        var act = async () => await _service.CalculateRentAsync(
            leaseId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{leaseId}*not found*");
    }

    #endregion

    #region Helper Methods

    private static Lease CreateLeaseWithTerms(Guid leaseId, dynamic[] termDefinitions)
    {
        var lease = new Lease
        {
            Id = leaseId,
            Terms = new List<LeaseTerm>()
        };

        foreach (var termDef in termDefinitions)
        {
            lease.Terms.Add(new LeaseTerm
            {
                Id = Guid.NewGuid(),
                LeaseId = leaseId,
                EffectiveFrom = termDef.From,
                EffectiveTo = termDef.To,
                MonthlyRent = termDef.Rent
            });
        }

        return lease;
    }

    #endregion
}

using TentMan.Application.Billing.Services;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
public class InvoiceRunServiceTests
{
    private readonly Mock<IInvoiceRunRepository> _mockInvoiceRunRepository;
    private readonly Mock<ILeaseRepository> _mockLeaseRepository;
    private readonly Mock<IInvoiceGenerationService> _mockInvoiceGenerationService;
    private readonly InvoiceRunService _service;

    public InvoiceRunServiceTests()
    {
        _mockInvoiceRunRepository = new Mock<IInvoiceRunRepository>();
        _mockLeaseRepository = new Mock<ILeaseRepository>();
        _mockInvoiceGenerationService = new Mock<IInvoiceGenerationService>();

        _service = new InvoiceRunService(
            _mockInvoiceRunRepository.Object,
            _mockLeaseRepository.Object,
            _mockInvoiceGenerationService.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ExecuteMonthlyRentRunAsync_WithActiveLeases_GeneratesInvoices()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var leases = CreateActiveLeases(orgId, 3);
        var invoiceResult = CreateSuccessfulInvoiceResult();

        _mockLeaseRepository
            .Setup(r => r.GetByOrgIdAsync(orgId, LeaseStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leases);

        _mockInvoiceGenerationService
            .Setup(s => s.GenerateInvoiceAsync(
                It.IsAny<Guid>(), billingStart, billingEnd, It.IsAny<ProrationMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoiceResult);

        _mockInvoiceRunRepository
            .Setup(r => r.AddAsync(It.IsAny<InvoiceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceRun run, CancellationToken _) => run);

        // Act
        var result = await _service.ExecuteMonthlyRentRunAsync(
            orgId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TotalLeases.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
        result.InvoiceRun.Should().NotBeNull();
        result.InvoiceRun!.Status.Should().Be(InvoiceRunStatus.Completed);

        _mockInvoiceGenerationService.Verify(
            s => s.GenerateInvoiceAsync(
                It.IsAny<Guid>(), billingStart, billingEnd, It.IsAny<ProrationMethod>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        _mockInvoiceRunRepository.Verify(
            r => r.AddAsync(It.IsAny<InvoiceRun>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteMonthlyRentRunAsync_NoActiveLeases_CompletesSuccessfully()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        _mockLeaseRepository
            .Setup(r => r.GetByOrgIdAsync(orgId, LeaseStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lease>());

        _mockInvoiceRunRepository
            .Setup(r => r.AddAsync(It.IsAny<InvoiceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceRun run, CancellationToken _) => run);

        // Act
        var result = await _service.ExecuteMonthlyRentRunAsync(
            orgId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TotalLeases.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.InvoiceRun!.Status.Should().Be(InvoiceRunStatus.Completed);

        _mockInvoiceGenerationService.Verify(
            s => s.GenerateInvoiceAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<ProrationMethod>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteMonthlyRentRunAsync_PartialFailures_CompletesWithErrors()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var leases = CreateActiveLeases(orgId, 3);
        var successResult = CreateSuccessfulInvoiceResult();
        var failureResult = CreateFailedInvoiceResult();

        _mockLeaseRepository
            .Setup(r => r.GetByOrgIdAsync(orgId, LeaseStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leases);

        // First two succeed, third fails
        _mockInvoiceGenerationService
            .SetupSequence(s => s.GenerateInvoiceAsync(
                It.IsAny<Guid>(), billingStart, billingEnd, It.IsAny<ProrationMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult)
            .ReturnsAsync(successResult)
            .ReturnsAsync(failureResult);

        _mockInvoiceRunRepository
            .Setup(r => r.AddAsync(It.IsAny<InvoiceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceRun run, CancellationToken _) => run);

        // Act
        var result = await _service.ExecuteMonthlyRentRunAsync(
            orgId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TotalLeases.Should().Be(3);
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(1);
        result.InvoiceRun!.Status.Should().Be(InvoiceRunStatus.CompletedWithErrors);
        result.ErrorMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteMonthlyRentRunAsync_AllFailures_MarksFailed()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 1, 1);
        var billingEnd = new DateOnly(2024, 1, 31);

        var leases = CreateActiveLeases(orgId, 2);
        var failureResult = CreateFailedInvoiceResult();

        _mockLeaseRepository
            .Setup(r => r.GetByOrgIdAsync(orgId, LeaseStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leases);

        _mockInvoiceGenerationService
            .Setup(s => s.GenerateInvoiceAsync(
                It.IsAny<Guid>(), billingStart, billingEnd, It.IsAny<ProrationMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        _mockInvoiceRunRepository
            .Setup(r => r.AddAsync(It.IsAny<InvoiceRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceRun run, CancellationToken _) => run);

        // Act
        var result = await _service.ExecuteMonthlyRentRunAsync(
            orgId, billingStart, billingEnd, ProrationMethod.ActualDaysInMonth);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TotalLeases.Should().Be(2);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(2);
        result.InvoiceRun!.Status.Should().Be(InvoiceRunStatus.Failed);
        result.ErrorMessages.Should().HaveCount(2);
    }

    #endregion

    #region Helper Methods

    private List<Lease> CreateActiveLeases(Guid orgId, int count)
    {
        var leases = new List<Lease>();
        for (int i = 0; i < count; i++)
        {
            leases.Add(new Lease
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                LeaseNumber = $"LEASE-{i + 1:000}",
                Status = LeaseStatus.Active,
                StartDate = new DateOnly(2024, 1, 1)
            });
        }
        return leases;
    }

    private InvoiceGenerationResult CreateSuccessfulInvoiceResult()
    {
        return new InvoiceGenerationResult
        {
            IsSuccess = true,
            Invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "INV-202401-000001",
                Status = InvoiceStatus.Draft
            },
            WasUpdated = false
        };
    }

    private InvoiceGenerationResult CreateFailedInvoiceResult()
    {
        return new InvoiceGenerationResult
        {
            IsSuccess = false,
            ErrorMessage = "Failed to generate invoice"
        };
    }

    #endregion
}

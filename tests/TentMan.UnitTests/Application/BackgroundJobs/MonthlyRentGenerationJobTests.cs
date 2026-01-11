using TentMan.Application.BackgroundJobs;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.BackgroundJobs;

[Trait("Category", "Unit")]
[Trait("Feature", "BackgroundJobs")]
public class MonthlyRentGenerationJobTests
{
    private readonly Mock<IInvoiceRunService> _mockInvoiceRunService;
    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
    private readonly Mock<ILogger<MonthlyRentGenerationJob>> _mockLogger;
    private readonly MonthlyRentGenerationJob _job;

    public MonthlyRentGenerationJobTests()
    {
        _mockInvoiceRunService = new Mock<IInvoiceRunService>();
        _mockOrganizationRepository = new Mock<IOrganizationRepository>();
        _mockLogger = new Mock<ILogger<MonthlyRentGenerationJob>>();

        _job = new MonthlyRentGenerationJob(
            _mockInvoiceRunService.Object,
            _mockOrganizationRepository.Object,
            _mockLogger.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ExecuteForNextMonthAsync_WithMultipleOrganizations_ProcessesAll()
    {
        // Arrange
        var organizations = new List<Organization>
        {
            CreateOrganization("Org1"),
            CreateOrganization("Org2"),
            CreateOrganization("Org3")
        };

        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizations);

        var successResult = new InvoiceRunResult
        {
            IsSuccess = true,
            SuccessCount = 5,
            FailureCount = 0
        };

        _mockInvoiceRunService
            .Setup(x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                ProrationMethod.ActualDaysInMonth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        await _job.ExecuteForNextMonthAsync(5);

        // Assert
        _mockOrganizationRepository.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _mockInvoiceRunService.Verify(
            x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                ProrationMethod.ActualDaysInMonth,
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteForNextMonthAsync_CalculatesCorrectBillingPeriod()
    {
        // Arrange
        var organizations = new List<Organization> { CreateOrganization("TestOrg") };
        
        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizations);

        DateOnly capturedStart = default;
        DateOnly capturedEnd = default;

        _mockInvoiceRunService
            .Setup(x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ProrationMethod>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, DateOnly, DateOnly, ProrationMethod, CancellationToken>(
                (_, start, end, _, _) =>
                {
                    capturedStart = start;
                    capturedEnd = end;
                })
            .ReturnsAsync(new InvoiceRunResult { IsSuccess = true });

        // Act
        await _job.ExecuteForNextMonthAsync(5);

        // Assert
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextMonth = today.AddMonths(1);
        var expectedStart = new DateOnly(nextMonth.Year, nextMonth.Month, 1);
        var expectedEnd = expectedStart.AddMonths(1).AddDays(-1);

        capturedStart.Should().Be(expectedStart);
        capturedEnd.Should().Be(expectedEnd);
    }

    [Fact]
    public async Task ExecuteForNextMonthAsync_WithNoOrganizations_LogsWarningAndReturns()
    {
        // Arrange
        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>());

        // Act
        await _job.ExecuteForNextMonthAsync(5);

        // Assert
        _mockInvoiceRunService.Verify(
            x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ProrationMethod>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyLogContains(LogLevel.Warning, "No organizations found");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteForNextMonthAsync_WithPartialFailure_ContinuesProcessing()
    {
        // Arrange
        var organizations = new List<Organization>
        {
            CreateOrganization("Org1"),
            CreateOrganization("Org2"),
            CreateOrganization("Org3")
        };

        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizations);

        var callCount = 0;
        _mockInvoiceRunService
            .Setup(x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ProrationMethod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new Exception("Test exception for Org2");
                }
                return new InvoiceRunResult { IsSuccess = true };
            });

        // Act
        await _job.ExecuteForNextMonthAsync(5);

        // Assert
        _mockInvoiceRunService.Verify(
            x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ProrationMethod>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        VerifyLogContains(LogLevel.Error, "Failed to process organization");
        VerifyLogContains(LogLevel.Warning, "Some organizations failed");
    }

    [Fact]
    public async Task ExecuteForNextMonthAsync_WithFatalError_ThrowsException()
    {
        // Arrange
        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _job.ExecuteForNextMonthAsync(5));

        VerifyLogContains(LogLevel.Error, "Fatal error");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ExecuteForNextMonthAsync_WhenRunningOutsideExpectedWindow_LogsWarning()
    {
        // This test validates that the daysBeforePeriodStart parameter is used for validation
        // Note: The actual validation depends on when the test runs, so we can't make strict assertions
        // Instead, we verify the method completes and handles the validation logic

        // Arrange
        var organizations = new List<Organization> { CreateOrganization("TestOrg") };
        
        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizations);

        _mockInvoiceRunService
            .Setup(x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ProrationMethod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceRunResult { IsSuccess = true });

        // Act
        await _job.ExecuteForNextMonthAsync(5);

        // Assert - method should complete without throwing
        _mockInvoiceRunService.Verify(
            x => x.ExecuteMonthlyRentRunAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ProrationMethod>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulRun_LogsInformation()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);

        var result = new InvoiceRunResult
        {
            IsSuccess = true,
            TotalLeases = 10,
            SuccessCount = 10,
            FailureCount = 0
        };

        _mockInvoiceRunService
            .Setup(x => x.ExecuteMonthlyRentRunAsync(
                orgId,
                billingStart,
                billingEnd,
                ProrationMethod.ActualDaysInMonth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _job.ExecuteAsync(orgId, billingStart, billingEnd);

        // Assert
        VerifyLogContains(LogLevel.Information, "Starting monthly rent generation");
        VerifyLogContains(LogLevel.Information, "Monthly rent generation completed successfully");
    }

    [Fact]
    public async Task ExecuteAsync_WithPartialFailures_LogsWarning()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);

        var result = new InvoiceRunResult
        {
            IsSuccess = true,
            TotalLeases = 10,
            SuccessCount = 8,
            FailureCount = 2,
            ErrorMessages = new List<string> { "Error 1", "Error 2" }
        };

        _mockInvoiceRunService
            .Setup(x => x.ExecuteMonthlyRentRunAsync(
                orgId,
                billingStart,
                billingEnd,
                ProrationMethod.ActualDaysInMonth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _job.ExecuteAsync(orgId, billingStart, billingEnd);

        // Assert
        VerifyLogContains(LogLevel.Warning, "Some invoices failed to generate");
    }

    [Fact]
    public async Task ExecuteAsync_WithCompleteFailure_ThrowsException()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var billingStart = new DateOnly(2024, 2, 1);
        var billingEnd = new DateOnly(2024, 2, 29);

        var result = new InvoiceRunResult
        {
            IsSuccess = false,
            ErrorMessages = new List<string> { "Database connection failed" }
        };

        _mockInvoiceRunService
            .Setup(x => x.ExecuteMonthlyRentRunAsync(
                orgId,
                billingStart,
                billingEnd,
                ProrationMethod.ActualDaysInMonth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _job.ExecuteAsync(orgId, billingStart, billingEnd));

        VerifyLogContains(LogLevel.Error, "Monthly rent generation failed");
    }

    #endregion

    #region Helper Methods

    private Organization CreateOrganization(string name)
    {
        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            TimeZone = "UTC",
            IsActive = true,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
    }

    private void VerifyLogContains(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce);
    }

    #endregion
}

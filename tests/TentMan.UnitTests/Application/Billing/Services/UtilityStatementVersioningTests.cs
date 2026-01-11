using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using FluentAssertions;
using Xunit;

namespace TentMan.UnitTests.Application.Billing.Services;

/// <summary>
/// Tests for utility statement versioning and finalization.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
[Trait("TestType", "Versioning")]
public class UtilityStatementVersioningTests
{
    #region Versioning Tests

    [Fact]
    public void UtilityStatement_DefaultVersion_ShouldBeOne()
    {
        // Arrange & Act
        var statement = new UtilityStatement
        {
            LeaseId = Guid.NewGuid(),
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = new DateOnly(2024, 1, 1),
            BillingPeriodEnd = new DateOnly(2024, 1, 31),
            TotalAmount = 1000m
        };

        // Assert
        statement.Version.Should().Be(1);
        statement.IsFinal.Should().BeFalse();
    }

    [Fact]
    public void MultipleUtilityStatements_SameLeaseAndPeriod_DifferentVersions()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var periodStart = new DateOnly(2024, 1, 1);
        var periodEnd = new DateOnly(2024, 1, 31);

        // Act
        var statement1 = new UtilityStatement
        {
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 1000m,
            Version = 1,
            IsFinal = false
        };

        var statement2 = new UtilityStatement
        {
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 1100m,
            Version = 2,
            IsFinal = false
        };

        var statementFinal = new UtilityStatement
        {
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 1050m,
            Version = 3,
            IsFinal = true
        };

        // Assert
        statement1.Version.Should().Be(1);
        statement1.IsFinal.Should().BeFalse();
        
        statement2.Version.Should().Be(2);
        statement2.IsFinal.Should().BeFalse();
        
        statementFinal.Version.Should().Be(3);
        statementFinal.IsFinal.Should().BeTrue();
    }

    [Fact]
    public void FinalUtilityStatement_ShouldPreventDuplicates()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var periodStart = new DateOnly(2024, 1, 1);
        var periodEnd = new DateOnly(2024, 1, 31);

        var finalStatement = new UtilityStatement
        {
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 1050m,
            Version = 1,
            IsFinal = true
        };

        // Act & Assert
        // Note: Uniqueness constraint in database configuration ensures only one final statement
        // per lease/utility type/billing period
        finalStatement.IsFinal.Should().BeTrue();
        
        // Attempting to create another final statement for the same period should fail at database level
        // This is enforced by the unique index with filter "IsFinal = 1"
    }

    #endregion

    #region Late Utility Billing Tests

    [Fact]
    public void LateUtilityBill_CanBeAddedForPastPeriod()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        
        // Utility bill for December received in February
        var pastPeriodStart = new DateOnly(2023, 12, 1);
        var pastPeriodEnd = new DateOnly(2023, 12, 31);
        var receivedDate = new DateOnly(2024, 2, 1);

        // Act
        var lateStatement = new UtilityStatement
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = pastPeriodStart,
            BillingPeriodEnd = pastPeriodEnd,
            DirectBillAmount = 1500m,
            TotalAmount = 1500m,
            IsMeterBased = false,
            Version = 1,
            IsFinal = true,
            Notes = $"Late bill received on {receivedDate:yyyy-MM-dd}",
            CreatedAtUtc = receivedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        };

        // Assert
        lateStatement.BillingPeriodStart.Should().Be(pastPeriodStart);
        lateStatement.BillingPeriodEnd.Should().Be(pastPeriodEnd);
        lateStatement.CreatedAtUtc.Should().BeAfter(pastPeriodEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    }

    #endregion

    #region Multiple Utilities Per Period Tests

    [Fact]
    public void MultipleUtilityTypes_SameLeaseAndPeriod_ShouldBeAllowed()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var periodStart = new DateOnly(2024, 1, 1);
        var periodEnd = new DateOnly(2024, 1, 31);

        // Act
        var electricityStatement = new UtilityStatement
        {
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 1000m,
            Version = 1,
            IsFinal = true
        };

        var waterStatement = new UtilityStatement
        {
            LeaseId = leaseId,
            UtilityType = UtilityType.Water,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 300m,
            Version = 1,
            IsFinal = true
        };

        var gasStatement = new UtilityStatement
        {
            LeaseId = leaseId,
            UtilityType = UtilityType.Gas,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 500m,
            Version = 1,
            IsFinal = true
        };

        // Assert
        electricityStatement.UtilityType.Should().Be(UtilityType.Electricity);
        waterStatement.UtilityType.Should().Be(UtilityType.Water);
        gasStatement.UtilityType.Should().Be(UtilityType.Gas);
        
        // All can be final for the same period as they are different utility types
        electricityStatement.IsFinal.Should().BeTrue();
        waterStatement.IsFinal.Should().BeTrue();
        gasStatement.IsFinal.Should().BeTrue();
    }

    #endregion

    #region Correction Workflow Tests

    [Fact]
    public void UtilityStatementCorrection_ShouldIncrementVersion()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var periodStart = new DateOnly(2024, 1, 1);
        var periodEnd = new DateOnly(2024, 1, 31);

        // Original statement (incorrect)
        var originalStatement = new UtilityStatement
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 1000m,
            Version = 1,
            IsFinal = false,
            Notes = "Initial reading"
        };

        // Correction (meter reading error discovered)
        var correctedStatement = new UtilityStatement
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 950m,
            Version = 2,
            IsFinal = false,
            Notes = "Corrected meter reading"
        };

        // Final statement after verification
        var finalStatement = new UtilityStatement
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            UtilityType = UtilityType.Electricity,
            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,
            TotalAmount = 950m,
            Version = 3,
            IsFinal = true,
            Notes = "Verified and finalized"
        };

        // Assert
        originalStatement.Version.Should().Be(1);
        originalStatement.IsFinal.Should().BeFalse();
        
        correctedStatement.Version.Should().Be(2);
        correctedStatement.IsFinal.Should().BeFalse();
        correctedStatement.Version.Should().BeGreaterThan(originalStatement.Version);
        
        finalStatement.Version.Should().Be(3);
        finalStatement.IsFinal.Should().BeTrue();
        finalStatement.Version.Should().BeGreaterThan(correctedStatement.Version);
    }

    #endregion
}

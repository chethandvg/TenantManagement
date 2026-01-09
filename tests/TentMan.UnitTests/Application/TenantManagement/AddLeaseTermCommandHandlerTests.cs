using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.TenantManagement.Leases.Commands.AddLeaseTerm;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement;

public class AddLeaseTermCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<AddLeaseTermCommandHandler>> _loggerMock;
    private readonly Mock<ILeaseRepository> _leaseRepositoryMock;
    private readonly AddLeaseTermCommandHandler _handler;

    public AddLeaseTermCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<AddLeaseTermCommandHandler>>();
        _leaseRepositoryMock = new Mock<ILeaseRepository>();

        _unitOfWorkMock.Setup(x => x.Leases).Returns(_leaseRepositoryMock.Object);

        _handler = new AddLeaseTermCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDuplicateEffectiveDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var existingTerm = new LeaseTerm
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            EffectiveFrom = effectiveFrom,
            MonthlyRent = 15000m,
            SecurityDeposit = 30000m
        };

        var lease = CreateTestLease(leaseId);
        lease.Terms.Add(existingTerm);

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        var command = new AddLeaseTermCommand(
            LeaseId: leaseId,
            EffectiveFrom: effectiveFrom, // Same date as existing term
            EffectiveTo: null,
            MonthlyRent: 16000m,
            SecurityDeposit: 32000m,
            MaintenanceCharge: null,
            OtherFixedCharge: null,
            EscalationType: EscalationType.None,
            EscalationValue: null,
            EscalationEveryMonths: null,
            Notes: null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("already exists for this lease", exception.Message);
        Assert.Contains(effectiveFrom.ToString("yyyy-MM-dd"), exception.Message);
    }

    [Fact]
    public async Task Handle_WhenEffectiveToBeforeEffectiveFrom_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveTo = effectiveFrom.AddDays(-1); // Before effective from

        var lease = CreateTestLease(leaseId);

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        var command = new AddLeaseTermCommand(
            LeaseId: leaseId,
            EffectiveFrom: effectiveFrom,
            EffectiveTo: effectiveTo, // Invalid - before EffectiveFrom
            MonthlyRent: 15000m,
            SecurityDeposit: 30000m,
            MaintenanceCharge: null,
            OtherFixedCharge: null,
            EscalationType: EscalationType.None,
            EscalationValue: null,
            EscalationEveryMonths: null,
            Notes: null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("EffectiveTo must be after EffectiveFrom", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenLeaseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow);

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lease?)null);

        var command = new AddLeaseTermCommand(
            LeaseId: leaseId,
            EffectiveFrom: effectiveFrom,
            EffectiveTo: null,
            MonthlyRent: 15000m,
            SecurityDeposit: 30000m,
            MaintenanceCharge: null,
            OtherFixedCharge: null,
            EscalationType: EscalationType.None,
            EscalationValue: null,
            EscalationEveryMonths: null,
            Notes: null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("not found", exception.Message);
    }

    private static Lease CreateTestLease(Guid leaseId)
    {
        return new Lease
        {
            Id = leaseId,
            OrgId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            LeaseNumber = "LSE-001",
            Status = LeaseStatus.Draft,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            RentDueDay = 1,
            GraceDays = 5,
            LateFeeType = LateFeeType.None,
            IsAutoRenew = false,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = new byte[] { 0x00 },
            Unit = new Unit { UnitNumber = "101", OccupancyStatus = OccupancyStatus.Vacant, Building = new Building { Name = "Test" } }
        };
    }
}

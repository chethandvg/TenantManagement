using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.TenantManagement.Leases.Commands.ActivateLease;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement;

public class ActivateLeaseCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<ActivateLeaseCommandHandler>> _loggerMock;
    private readonly Mock<ILeaseRepository> _leaseRepositoryMock;
    private readonly ActivateLeaseCommandHandler _handler;

    public ActivateLeaseCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<ActivateLeaseCommandHandler>>();
        _leaseRepositoryMock = new Mock<ILeaseRepository>();

        _unitOfWorkMock.Setup(x => x.Leases).Returns(_leaseRepositoryMock.Object);

        // Setup ExecuteWithRetryAsync to execute the function directly
        _unitOfWorkMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task<Contracts.Leases.LeaseDetailDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Contracts.Leases.LeaseDetailDto>> func, CancellationToken _) => func());

        _handler = new ActivateLeaseCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenLeaseNotInDraftStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var lease = CreateTestLease(leaseId);
        lease.Status = LeaseStatus.Active; // Not draft

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);

        var command = new ActivateLeaseCommand(leaseId, new byte[] { 0x00 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Draft status", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenUnitHasActiveLeaseAlready_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var lease = CreateValidDraftLease(leaseId);

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        _leaseRepositoryMock.Setup(x => x.HasActiveLeaseAsync(lease.UnitId, leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ActivateLeaseCommand(leaseId, new byte[] { 0x00 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("active lease", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenNoPrimaryTenant_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var lease = CreateTestLease(leaseId);
        lease.Status = LeaseStatus.Draft;
        // No parties added

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        _leaseRepositoryMock.Setup(x => x.HasActiveLeaseAsync(lease.UnitId, leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ActivateLeaseCommand(leaseId, new byte[] { 0x00 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("Primary Tenant", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenNoTermCoveringStartDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var lease = CreateTestLease(leaseId);
        lease.Status = LeaseStatus.Draft;
        lease.Parties.Add(new LeaseParty
        {
            Role = LeasePartyRole.PrimaryTenant,
            TenantId = Guid.NewGuid(),
            Tenant = new Tenant { FullName = "Test", Phone = "123" }
        });
        // No terms covering start date

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        _leaseRepositoryMock.Setup(x => x.HasActiveLeaseAsync(lease.UnitId, leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ActivateLeaseCommand(leaseId, new byte[] { 0x00 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("term covering the start date", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenRentDueDayOutOfRange_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var lease = CreateValidDraftLease(leaseId);
        lease.RentDueDay = 31; // Invalid

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        _leaseRepositoryMock.Setup(x => x.HasActiveLeaseAsync(lease.UnitId, leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ActivateLeaseCommand(leaseId, new byte[] { 0x00 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("RentDueDay", exception.Message);
    }

    [Fact]
    public async Task Handle_WhenEndDateBeforeStartDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var leaseId = Guid.NewGuid();
        var lease = CreateValidDraftLease(leaseId);
        lease.EndDate = lease.StartDate.AddDays(-1); // Invalid

        _leaseRepositoryMock.Setup(x => x.GetByIdWithDetailsAsync(leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        _leaseRepositoryMock.Setup(x => x.HasActiveLeaseAsync(lease.UnitId, leaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ActivateLeaseCommand(leaseId, new byte[] { 0x00 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.Contains("EndDate must be after StartDate", exception.Message);
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

    private static Lease CreateValidDraftLease(Guid leaseId)
    {
        var lease = CreateTestLease(leaseId);
        lease.Status = LeaseStatus.Draft;
        lease.RentDueDay = 1;
        
        // Add primary tenant
        lease.Parties.Add(new LeaseParty
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            TenantId = Guid.NewGuid(),
            Role = LeasePartyRole.PrimaryTenant,
            IsResponsibleForPayment = true,
            Tenant = new Tenant { FullName = "Test Tenant", Phone = "1234567890" }
        });

        // Add term covering start date
        lease.Terms.Add(new LeaseTerm
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            EffectiveFrom = lease.StartDate.AddDays(-1),
            MonthlyRent = 15000m,
            SecurityDeposit = 30000m
        });

        return lease;
    }
}

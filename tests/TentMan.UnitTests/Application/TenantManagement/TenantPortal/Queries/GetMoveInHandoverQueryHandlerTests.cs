using TentMan.Application.Abstractions;
using TentMan.Application.TenantManagement.TenantPortal.Queries;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.TenantManagement.TenantPortal.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "MoveInHandover")]
public class GetMoveInHandoverQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<GetMoveInHandoverQueryHandler>> _mockLogger;
    private readonly GetMoveInHandoverQueryHandler _handler;

    public GetMoveInHandoverQueryHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<GetMoveInHandoverQueryHandler>>();

        _handler = new GetMoveInHandoverQueryHandler(
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_NoTenantFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetMoveInHandoverQuery(userId);

        _mockUnitOfWork.Setup(u => u.Tenants.GetByLinkedUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoActiveLease_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var query = new GetMoveInHandoverQuery(userId);

        var tenant = new Tenant { Id = tenantId };

        _mockUnitOfWork.Setup(u => u.Tenants.GetByLinkedUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidHandover_ReturnsHandoverResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var handoverId = Guid.NewGuid();
        var query = new GetMoveInHandoverQuery(userId);

        var lease = new Lease
        {
            Id = leaseId,
            Status = LeaseStatus.Active,
            Unit = new Unit { UnitNumber = "A-101", Building = new Building { Name = "Building A" } }
        };

        var tenant = new Tenant
        {
            Id = tenantId,
            LeaseParties = new List<LeaseParty>
            {
                new LeaseParty { TenantId = tenantId, LeaseId = leaseId, Lease = lease, IsDeleted = false }
            }
        };

        var handover = new UnitHandover
        {
            Id = handoverId,
            LeaseId = leaseId,
            Type = HandoverType.MoveIn,
            Date = DateOnly.FromDateTime(DateTime.Today),
            SignedByTenant = false,
            Lease = lease,
            ChecklistItems = new List<HandoverChecklistItem>()
        };

        _mockUnitOfWork.Setup(u => u.Tenants.GetByLinkedUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockUnitOfWork.Setup(u => u.UnitHandovers.GetByLeaseIdAsync(leaseId, HandoverType.MoveIn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handover);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.HandoverId.Should().Be(handoverId);
        result.LeaseId.Should().Be(leaseId);
        result.UnitNumber.Should().Be("A-101");
        result.BuildingName.Should().Be("Building A");
        result.IsCompleted.Should().BeFalse();
    }
}

using TentMan.Application.Abstractions;
using TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingOwnership;
using TentMan.Application.PropertyManagement.Services;
using TentMan.Contracts.Buildings;
using TentMan.Domain.Entities;
using TentMan.UnitTests.TestHelpers.Fixtures;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.PropertyManagement.Buildings.Commands;

public class SetBuildingOwnershipCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly IOwnershipService _ownershipService;
    private readonly SetBuildingOwnershipCommandHandler _handler;

    public SetBuildingOwnershipCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _ownershipService = new OwnershipService();
        _handler = new SetBuildingOwnershipCommandHandler(
            _unitOfWorkMock.Object,
            _ownershipService,
            _currentUserMock.Object,
            new Mock<ILogger<SetBuildingOwnershipCommandHandler>>().Object);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WithValidShares_ShouldSetOwnership(
        Building building,
        Owner owner1,
        Owner owner2,
        Guid buildingId,
        CancellationToken cancellationToken)
    {
        // Arrange
        building.Id = buildingId;
        building.OwnershipShares.Clear();
        
        var shares = new List<OwnershipShareRequest>
        {
            new() { OwnerId = owner1.Id, SharePercent = 60.00m },
            new() { OwnerId = owner2.Id, SharePercent = 40.00m }
        };

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            shares,
            DateTime.UtcNow);

        _unitOfWorkMock.Setup(x => x.Buildings.GetByIdAsync(buildingId, cancellationToken))
            .ReturnsAsync(building);
        _unitOfWorkMock.Setup(x => x.Owners.ExistsAsync(owner1.Id, cancellationToken))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.Owners.ExistsAsync(owner2.Id, cancellationToken))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
        building.OwnershipShares.Should().HaveCount(2);
        building.OwnershipShares.Sum(s => s.SharePercent).Should().Be(100.00m);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WithInvalidSum_ShouldThrowException(
        Building building,
        Owner owner1,
        Owner owner2,
        Guid buildingId,
        CancellationToken cancellationToken)
    {
        // Arrange
        building.Id = buildingId;
        
        var shares = new List<OwnershipShareRequest>
        {
            new() { OwnerId = owner1.Id, SharePercent = 60.00m },
            new() { OwnerId = owner2.Id, SharePercent = 30.00m } // Sum = 90, not 100
        };

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            shares,
            DateTime.UtcNow);

        _unitOfWorkMock.Setup(x => x.Buildings.GetByIdAsync(buildingId, cancellationToken))
            .ReturnsAsync(building);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, cancellationToken));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WithDuplicateOwners_ShouldThrowException(
        Building building,
        Owner owner1,
        Guid buildingId,
        CancellationToken cancellationToken)
    {
        // Arrange
        building.Id = buildingId;
        
        var shares = new List<OwnershipShareRequest>
        {
            new() { OwnerId = owner1.Id, SharePercent = 50.00m },
            new() { OwnerId = owner1.Id, SharePercent = 50.00m } // Same owner twice
        };

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            shares,
            DateTime.UtcNow);

        _unitOfWorkMock.Setup(x => x.Buildings.GetByIdAsync(buildingId, cancellationToken))
            .ReturnsAsync(building);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, cancellationToken));
    }

    [Theory, AutoMoqData]
    public async Task Handle_WithNegativeShare_ShouldThrowException(
        Building building,
        Owner owner1,
        Guid buildingId,
        CancellationToken cancellationToken)
    {
        // Arrange
        building.Id = buildingId;
        
        var shares = new List<OwnershipShareRequest>
        {
            new() { OwnerId = owner1.Id, SharePercent = 0.00m } // Zero or negative not allowed
        };

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            shares,
            DateTime.UtcNow);

        _unitOfWorkMock.Setup(x => x.Buildings.GetByIdAsync(buildingId, cancellationToken))
            .ReturnsAsync(building);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, cancellationToken));
    }
}

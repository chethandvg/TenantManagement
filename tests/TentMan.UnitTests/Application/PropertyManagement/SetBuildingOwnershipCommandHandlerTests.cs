using FluentAssertions;
using Moq;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingOwnership;
using TentMan.Application.PropertyManagement.Services;
using TentMan.Contracts.Buildings;
using TentMan.Domain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;

namespace TentMan.UnitTests.Application.PropertyManagement;

public class SetBuildingOwnershipCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IBuildingRepository> _buildingRepositoryMock;
    private readonly Mock<IOwnerRepository> _ownerRepositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<SetBuildingOwnershipCommandHandler>> _loggerMock;
    private readonly IOwnershipService _ownershipService;
    private readonly SetBuildingOwnershipCommandHandler _handler;

    public SetBuildingOwnershipCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _buildingRepositoryMock = new Mock<IBuildingRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<SetBuildingOwnershipCommandHandler>>();
        _ownershipService = new OwnershipService();

        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Owners).Returns(_ownerRepositoryMock.Object);

        _currentUserMock.Setup(c => c.UserId).Returns("test-user-id");

        _handler = new SetBuildingOwnershipCommandHandler(
            _unitOfWorkMock.Object,
            _ownershipService,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSharesSumTo100_ShouldSucceed()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var ownerId1 = Guid.NewGuid();
        var ownerId2 = Guid.NewGuid();

        var building = new Building
        {
            Id = buildingId,
            OrgId = Guid.NewGuid(),
            BuildingCode = "B001",
            Name = "Test Building",
            OwnershipShares = new List<BuildingOwnershipShare>()
        };

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            new List<OwnershipShareRequest>
            {
                new() { OwnerId = ownerId1, SharePercent = 60.00m },
                new() { OwnerId = ownerId2, SharePercent = 40.00m }
            },
            DateTime.UtcNow);

        _buildingRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(building);

        _ownerRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WhenSharesSumNot100_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var ownerId1 = Guid.NewGuid();
        var ownerId2 = Guid.NewGuid();

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            new List<OwnershipShareRequest>
            {
                new() { OwnerId = ownerId1, SharePercent = 60.00m },
                new() { OwnerId = ownerId2, SharePercent = 30.00m } // Sum = 90, not 100
            },
            DateTime.UtcNow);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("100");
    }

    [Fact]
    public async Task Handle_WhenDuplicateOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            new List<OwnershipShareRequest>
            {
                new() { OwnerId = ownerId, SharePercent = 50.00m },
                new() { OwnerId = ownerId, SharePercent = 50.00m } // Duplicate owner
            },
            DateTime.UtcNow);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("only appear once");
    }

    [Fact]
    public async Task Handle_WhenShareIsZeroOrNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var ownerId1 = Guid.NewGuid();
        var ownerId2 = Guid.NewGuid();

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            new List<OwnershipShareRequest>
            {
                new() { OwnerId = ownerId1, SharePercent = 100.00m },
                new() { OwnerId = ownerId2, SharePercent = 0m } // Zero share
            },
            DateTime.UtcNow);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("greater than 0");
    }

    [Fact]
    public async Task Handle_WhenNoShares_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            new List<OwnershipShareRequest>(), // Empty list
            DateTime.UtcNow);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("At least one ownership share is required");
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            new List<OwnershipShareRequest>
            {
                new() { OwnerId = ownerId, SharePercent = 100.00m }
            },
            DateTime.UtcNow);

        _buildingRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Building?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenOwnerNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var building = new Building
        {
            Id = buildingId,
            OrgId = Guid.NewGuid(),
            BuildingCode = "B001",
            Name = "Test Building",
            OwnershipShares = new List<BuildingOwnershipShare>()
        };

        var command = new SetBuildingOwnershipCommand(
            buildingId,
            new List<OwnershipShareRequest>
            {
                new() { OwnerId = ownerId, SharePercent = 100.00m }
            },
            DateTime.UtcNow);

        _buildingRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(building);

        _ownerRepositoryMock
            .Setup(r => r.ExistsAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    [Theory]
    [InlineData(99.99, 0.01)] // Total = 100.00, edge case
    [InlineData(33.33, 33.33, 33.34)] // Total = 100.00 exactly
    [InlineData(100.005 - 0.005, 0.005)] // Near tolerance
    public async Task Handle_WhenSharesSumWithinTolerance_ShouldSucceed(params double[] shares)
    {
        // Arrange
        var buildingId = Guid.NewGuid();

        var building = new Building
        {
            Id = buildingId,
            OrgId = Guid.NewGuid(),
            BuildingCode = "B001",
            Name = "Test Building",
            OwnershipShares = new List<BuildingOwnershipShare>()
        };

        var ownershipShares = shares.Select(s => new OwnershipShareRequest
        {
            OwnerId = Guid.NewGuid(),
            SharePercent = (decimal)s
        }).ToList();

        var command = new SetBuildingOwnershipCommand(buildingId, ownershipShares, DateTime.UtcNow);

        _buildingRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(building);

        _ownerRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}

using FluentAssertions;
using Moq;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.PropertyManagement.Units.Commands.BulkCreateUnits;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using Microsoft.Extensions.Logging;
using Xunit;

namespace TentMan.UnitTests.Application.PropertyManagement;

public class BulkCreateUnitsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IBuildingRepository> _buildingRepositoryMock;
    private readonly Mock<IUnitRepository> _unitRepositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<BulkCreateUnitsCommandHandler>> _loggerMock;
    private readonly BulkCreateUnitsCommandHandler _handler;

    public BulkCreateUnitsCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _buildingRepositoryMock = new Mock<IBuildingRepository>();
        _unitRepositoryMock = new Mock<IUnitRepository>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<BulkCreateUnitsCommandHandler>>();

        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Units).Returns(_unitRepositoryMock.Object);

        _currentUserMock.Setup(c => c.UserId).Returns("test-user-id");

        _handler = new BulkCreateUnitsCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidUnits_ShouldCreateAllUnits()
    {
        // Arrange
        var buildingId = Guid.NewGuid();

        var command = new BulkCreateUnitsCommand(
            buildingId,
            new List<CreateUnitData>
            {
                new("101", 1, UnitType.TwoBHK, 1000m, 2, 1, Furnishing.SemiFurnished, 1),
                new("102", 1, UnitType.ThreeBHK, 1200m, 3, 2, Furnishing.FullyFurnished, 2)
            });

        _buildingRepositoryMock
            .Setup(r => r.ExistsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitRepositoryMock
            .Setup(r => r.UnitNumberExistsAsync(buildingId, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _unitRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Unit>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDuplicateUnitNumbersInRequest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();

        var command = new BulkCreateUnitsCommand(
            buildingId,
            new List<CreateUnitData>
            {
                new("101", 1, UnitType.TwoBHK, 1000m, 2, 1, Furnishing.SemiFurnished, 1),
                new("101", 1, UnitType.ThreeBHK, 1200m, 3, 2, Furnishing.FullyFurnished, 2) // Duplicate
            });

        _buildingRepositoryMock
            .Setup(r => r.ExistsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Duplicate unit numbers");
        exception.Message.Should().Contain("101");
    }

    [Fact]
    public async Task Handle_WhenBuildingNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();

        var command = new BulkCreateUnitsCommand(
            buildingId,
            new List<CreateUnitData>
            {
                new("101", 1, UnitType.TwoBHK, 1000m, 2, 1, Furnishing.SemiFurnished, 1)
            });

        _buildingRepositoryMock
            .Setup(r => r.ExistsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenUnitNumberExistsInBuilding_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var buildingId = Guid.NewGuid();

        var command = new BulkCreateUnitsCommand(
            buildingId,
            new List<CreateUnitData>
            {
                new("101", 1, UnitType.TwoBHK, 1000m, 2, 1, Furnishing.SemiFurnished, 1)
            });

        _buildingRepositoryMock
            .Setup(r => r.ExistsAsync(buildingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitRepositoryMock
            .Setup(r => r.UnitNumberExistsAsync(buildingId, "101", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Unit already exists

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("already exists");
    }
}

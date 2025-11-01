using System.Collections.Generic;
using System.Threading;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Commands.RemovePermissionFromUser;
using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class RemovePermissionFromUserCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_ShouldRemovePermissions_WhenAssignmentsExist(
        Guid userId,
        string permissionName)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        var userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();

        var permission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            NormalizedName = permissionName.ToUpperInvariant()
        };

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId });

        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission });

        userPermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission.NormalizedName });

        IEnumerable<Guid>? removedPermissions = null;
        userPermissionRepositoryMock
            .Setup(repo => repo.UnlinkPermissionsAsync(userId, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IEnumerable<Guid>, CancellationToken>((_, ids, _) => removedPermissions = ids.ToArray())
            .Returns(Task.CompletedTask);

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromUserCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserPermissions).Returns(userPermissionRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromUserCommand(userId, new[] { permissionName });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        removedPermissions.Should().NotBeNull();
        removedPermissions!.Should().ContainSingle(id => id == permission.Id);
        result.Permissions.Should().BeEmpty();
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenUserNotFound(Guid userId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromUserCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromUserCommand(userId, new[] { "Permission" });

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenPermissionsMissing(Guid userId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var permissionRepositoryMock = new Mock<IPermissionRepository>();

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId });

        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApplicationPermission>());

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromUserCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromUserCommand(userId, new[] { "Missing" });

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenPermissionsNotAssigned(Guid userId, string permissionName)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        var userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();

        var permission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            NormalizedName = permissionName.ToUpperInvariant()
        };

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId });

        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission });

        userPermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromUserCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserPermissions).Returns(userPermissionRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromUserCommand(userId, new[] { permissionName });

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Creates a fixture configured for the remove permission from user handler.
    /// </summary>
    private static CommandHandlerTestFixture<RemovePermissionFromUserCommandHandler> CreateFixture()
    {
        return new CommandHandlerTestFixture<RemovePermissionFromUserCommandHandler>();
    }
}

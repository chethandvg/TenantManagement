using System.Collections.Generic;
using System.Threading;
using Archu.Application.Abstractions;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Commands.RemovePermissionFromRole;
using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class RemovePermissionFromRoleCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_ShouldRemovePermissions_WhenAssignmentsExist(
        Guid roleId,
        string permissionName)
    {
        // Arrange
        var permission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            NormalizedName = permissionName.ToUpperInvariant()
        };

        var roleRepositoryMock = new Mock<IRoleRepository>();
        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        var rolePermissionRepositoryMock = new Mock<IRolePermissionRepository>();

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = roleId });

        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission });

        rolePermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByRoleIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission.NormalizedName });

        IEnumerable<Guid>? removedPermissions = null;
        rolePermissionRepositoryMock
            .Setup(repo => repo.UnlinkPermissionsAsync(roleId, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IEnumerable<Guid>, CancellationToken>((_, ids, _) => removedPermissions = ids.ToArray())
            .Returns(Task.CompletedTask);

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromRoleCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.RolePermissions).Returns(rolePermissionRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromRoleCommand(roleId, new[] { permissionName });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        removedPermissions.Should().NotBeNull();
        removedPermissions!.Should().ContainSingle(id => id == permission.Id);
        result.Permissions.Should().BeEmpty();
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenRoleNotFound(Guid roleId)
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>();
        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationRole?)null);

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromRoleCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromRoleCommand(roleId, new[] { "Permission" });

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenPermissionsMissing(Guid roleId)
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var permissionRepositoryMock = new Mock<IPermissionRepository>();

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = roleId });

        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApplicationPermission>());

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromRoleCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromRoleCommand(roleId, new[] { "Missing" });

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldThrow_WhenPermissionsNotAssigned(Guid roleId, string permissionName)
    {
        // Arrange
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var permissionRepositoryMock = new Mock<IPermissionRepository>();
        var rolePermissionRepositoryMock = new Mock<IRolePermissionRepository>();

        var permission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            NormalizedName = permissionName.ToUpperInvariant()
        };

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = roleId });

        permissionRepositoryMock
            .Setup(repo => repo.GetByNormalizedNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { permission });

        rolePermissionRepositoryMock
            .Setup(repo => repo.GetPermissionNamesByRoleIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var fixture = CreateFixture()
            .WithHandlerFactory((unitOfWork, _, logger) => new RemovePermissionFromRoleCommandHandler(unitOfWork, logger));

        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Permissions).Returns(permissionRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.RolePermissions).Returns(rolePermissionRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new RemovePermissionFromRoleCommand(roleId, new[] { permissionName });

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Creates a fixture configured for the remove permission from role handler.
    /// </summary>
    private static CommandHandlerTestFixture<RemovePermissionFromRoleCommandHandler> CreateFixture()
    {
        return new CommandHandlerTestFixture<RemovePermissionFromRoleCommandHandler>();
    }
}

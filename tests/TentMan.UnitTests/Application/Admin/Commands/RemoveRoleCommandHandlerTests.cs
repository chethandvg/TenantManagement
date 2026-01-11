using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Admin.Commands.RemoveRole;
using TentMan.Application.Common;
using TentMan.Shared.Constants.Authorization;
using TentMan.Domain.Entities.Identity;
using TentMan.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class RemoveRoleCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenRequestValid_ShouldRemoveRole(
        Guid userId,
        Guid roleId,
        Guid adminId,
        string userName,
        string roleName)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var user = new ApplicationUser { Id = userId, UserName = userName };
        var role = new ApplicationRole { Id = roleId, Name = roleName, NormalizedName = roleName.ToUpperInvariant() };

        var fixture = new CommandHandlerTestFixture<RemoveRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(true);
        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.Administrator)).Returns(false);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.SuperAdmin });

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        CancellationToken capturedGetUserToken = default;
        CancellationToken capturedGetRoleToken = default;
        CancellationToken capturedHasRoleToken = default;
        CancellationToken capturedRemoveToken = default;
        CancellationToken capturedSaveToken = default;

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedGetUserToken = token)
            .ReturnsAsync(user);

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedGetRoleToken = token)
            .ReturnsAsync(role);

        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((_, _, token) => capturedHasRoleToken = token)
            .ReturnsAsync(true);

        userRoleRepositoryMock
            .Setup(repo => repo.RemoveAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((_, _, token) => capturedRemoveToken = token)
            .Returns(Task.CompletedTask);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedSaveToken = token)
            .ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        var handler = fixture.CreateHandler();
        var command = new RemoveRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.Should().Be(Result.Success());

        userRoleRepositoryMock.Verify(repo => repo.RemoveAsync(userId, roleId, cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(cancellationTokenSource.Token), Times.Once());

        capturedGetUserToken.Should().Be(cancellationTokenSource.Token);
        capturedGetRoleToken.Should().Be(cancellationTokenSource.Token);
        capturedHasRoleToken.Should().Be(cancellationTokenSource.Token);
        capturedRemoveToken.Should().Be(cancellationTokenSource.Token);
        capturedSaveToken.Should().Be(cancellationTokenSource.Token);

        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["RoleName"] = roleName,
                ["UserName"] = userName,
                ["AdminUserId"] = adminId.ToString()
            },
            Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure(Guid userId, Guid roleId, Guid adminId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = new CommandHandlerTestFixture<RemoveRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        CancellationToken capturedToken = default;
        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedToken = token)
            .ReturnsAsync((ApplicationUser?)null);

        var handler = fixture.CreateHandler();
        var command = new RemoveRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"User with ID {userId} not found");
        capturedToken.Should().Be(CancellationToken.None);

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?> { ["UserId"] = userId },
            Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRoleNotFound_ShouldReturnFailure(Guid userId, Guid roleId, Guid adminId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = new CommandHandlerTestFixture<RemoveRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId });

        CancellationToken capturedToken = default;
        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedToken = token)
            .ReturnsAsync((ApplicationRole?)null);

        var handler = fixture.CreateHandler();
        var command = new RemoveRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"Role with ID {roleId} not found");
        capturedToken.Should().Be(CancellationToken.None);

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?> { ["RoleId"] = roleId },
            Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserDoesNotHaveRole_ShouldReturnFailure(
        Guid userId,
        Guid roleId,
        Guid adminId,
        string userName,
        string roleName)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();

        var fixture = new CommandHandlerTestFixture<RemoveRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, UserName = userName });

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = roleId, Name = roleName });

        CancellationToken capturedToken = default;
        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((_, _, token) => capturedToken = token)
            .ReturnsAsync(false);

        var handler = fixture.CreateHandler();
        var command = new RemoveRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"User '{userName}' does not have the role '{roleName}'");
        capturedToken.Should().Be(CancellationToken.None);

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["UserId"] = userId,
                ["RoleId"] = roleId
            },
            Times.Once());

        userRoleRepositoryMock.Verify(repo => repo.RemoveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenAdminRemovesOwnAdministratorRole_ShouldFail(Guid adminId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();

        var fixture = new CommandHandlerTestFixture<RemoveRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(false);
        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.Administrator)).Returns(true);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.Administrator });

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new RemoveRoleCommand(adminId, Guid.NewGuid());

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.RoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = command.RoleId, Name = RoleNames.Administrator });

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = command.UserId, UserName = "admin" });

        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(command.UserId, command.RoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("You cannot remove your own Administrator role");

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["AdminUserId"] = adminId.ToString(),
                ["RoleName"] = RoleNames.Administrator,
                ["Reason"] = "Security restriction: You cannot remove your own Administrator role. This prevents accidental loss of administrative privileges. Another Administrator or SuperAdmin must remove this role."
            },
            Times.Once());

        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenAdministratorAttemptsToRemoveSuperAdminRole_ShouldFail(
        Guid adminId,
        Guid targetUserId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();

        var fixture = new CommandHandlerTestFixture<RemoveRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(false);
        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.Administrator)).Returns(true);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.Administrator });

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = targetUserId, UserName = "target" });

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = Guid.NewGuid(), Name = RoleNames.SuperAdmin });

        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(targetUserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = fixture.CreateHandler();
        var command = new RemoveRoleCommand(targetUserId, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only SuperAdmin can remove the 'SuperAdmin' role");

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["AdminUserId"] = adminId.ToString(),
                ["RoleName"] = RoleNames.SuperAdmin,
                ["Reason"] = "Permission denied: Only SuperAdmin can remove the 'SuperAdmin' role. Administrators cannot demote SuperAdmin users."
            },
            Times.Once());

        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRemovingLastSuperAdminRole_ShouldFail(
        Guid adminId,
        Guid targetUserId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var superAdminRole = new ApplicationRole { Id = Guid.NewGuid(), Name = RoleNames.SuperAdmin };

        var fixture = new CommandHandlerTestFixture<RemoveRoleCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(true);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.SuperAdmin });

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = targetUserId, UserName = "super" });

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(superAdminRole);

        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(targetUserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        CancellationToken capturedCountToken = default;
        userRoleRepositoryMock
            .Setup(repo => repo.CountUsersWithRoleAsync(superAdminRole.Id, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedCountToken = token)
            .ReturnsAsync(1);

        var handler = fixture.CreateHandler();
        var command = new RemoveRoleCommand(targetUserId, superAdminRole.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot remove the last SuperAdmin role");
        capturedCountToken.Should().Be(CancellationToken.None);

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["Reason"] = "Critical security restriction: Cannot remove the last SuperAdmin role from the system. At least one SuperAdmin must exist to maintain system administration capabilities. Please assign SuperAdmin role to another user before removing it from this user."
            },
            Times.Once());

        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

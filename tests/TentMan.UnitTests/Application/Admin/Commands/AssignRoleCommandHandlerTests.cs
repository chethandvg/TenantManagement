using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Admin.Commands.AssignRole;
using TentMan.Application.Common;
using TentMan.Domain.Constants;
using TentMan.Domain.Entities.Identity;
using TentMan.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class AssignRoleCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenRequestIsValid_ShouldAssignRole(
        Guid userId,
        Guid roleId,
        Guid adminId,
        string userName,
        string roleName,
        DateTime utcNow)
    {
        // Arrange
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var user = new ApplicationUser { Id = userId, UserName = userName };
        var role = new ApplicationRole { Id = roleId, Name = roleName, NormalizedName = roleName.ToUpperInvariant() };

        var fixture = CreateFixture(timeProviderMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(true);
        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.Administrator)).Returns(false);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.SuperAdmin });

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        timeProviderMock.SetupGet(provider => provider.UtcNow).Returns(utcNow);

        CancellationToken capturedUserToken = default;
        CancellationToken capturedRoleToken = default;
        CancellationToken capturedHasRoleToken = default;
        CancellationToken capturedAddToken = default;
        CancellationToken capturedSaveToken = default;
        UserRole? capturedUserRole = null;

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedUserToken = token)
            .ReturnsAsync(user);

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedRoleToken = token)
            .ReturnsAsync(role);

        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((_, _, token) => capturedHasRoleToken = token)
            .ReturnsAsync(false);

        userRoleRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .Callback<UserRole, CancellationToken>((userRole, token) =>
            {
                capturedAddToken = token;
                capturedUserRole = userRole;
            })
            .Returns(Task.CompletedTask);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedSaveToken = token)
            .ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        var handler = fixture.CreateHandler();
        var command = new AssignRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.Should().Be(Result.Success());

        capturedUserRole.Should().NotBeNull();
        capturedUserRole!.UserId.Should().Be(userId);
        capturedUserRole.RoleId.Should().Be(roleId);
        capturedUserRole.AssignedAtUtc.Should().Be(utcNow);
        capturedUserRole.AssignedBy.Should().Be(adminId.ToString());

        userRoleRepositoryMock.Verify(repo => repo.AddAsync(It.Is<UserRole>(ur => ur == capturedUserRole), cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(cancellationTokenSource.Token), Times.Once());

        capturedUserToken.Should().Be(cancellationTokenSource.Token);
        capturedRoleToken.Should().Be(cancellationTokenSource.Token);
        capturedHasRoleToken.Should().Be(cancellationTokenSource.Token);
        capturedAddToken.Should().Be(cancellationTokenSource.Token);
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
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = CreateFixture(timeProviderMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(Array.Empty<string>());
        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        CancellationToken capturedToken = default;
        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedToken = token)
            .ReturnsAsync((ApplicationUser?)null);

        var handler = fixture.CreateHandler();
        var command = new AssignRoleCommand(userId, roleId);

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
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = CreateFixture(timeProviderMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(Array.Empty<string>());
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
        var command = new AssignRoleCommand(userId, roleId);

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
    public async Task Handle_WhenUserAlreadyHasRole_ShouldReturnFailure(
        Guid userId,
        Guid roleId,
        Guid adminId,
        string userName,
        string roleName)
    {
        // Arrange
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();

        var fixture = CreateFixture(timeProviderMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(true);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.SuperAdmin });
        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        var user = new ApplicationUser { Id = userId, UserName = userName };
        var role = new ApplicationRole { Id = roleId, Name = roleName };

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        CancellationToken capturedToken = default;
        userRoleRepositoryMock
            .Setup(repo => repo.UserHasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((_, _, token) => capturedToken = token)
            .ReturnsAsync(true);

        var handler = fixture.CreateHandler();
        var command = new AssignRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be($"User '{userName}' already has the role '{roleName}'");
        capturedToken.Should().Be(CancellationToken.None);

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["UserId"] = userId,
                ["RoleId"] = roleId
            },
            Times.Once());

        userRoleRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenAdministratorAssignsSuperAdminRole_ShouldReturnFailure(
        Guid userId,
        Guid roleId,
        Guid adminId)
    {
        // Arrange
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = CreateFixture(timeProviderMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(false);
        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.Administrator)).Returns(true);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.Administrator });

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, UserName = "target" });

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = roleId, Name = RoleNames.SuperAdmin });

        var handler = fixture.CreateHandler();
        var command = new AssignRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only SuperAdmin can assign the 'SuperAdmin' role");

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["AdminUserId"] = adminId.ToString(),
                ["RoleName"] = RoleNames.SuperAdmin,
                ["AdminRoles"] = RoleNames.Administrator,
                ["Reason"] = "Permission denied: Only SuperAdmin can assign the 'SuperAdmin' role. Administrators cannot elevate users to SuperAdmin status."
            },
            Times.Once());

        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNotPrivileged_ShouldReturnFailure(
        Guid userId,
        Guid roleId,
        Guid adminId)
    {
        // Arrange
        var timeProviderMock = new Mock<ITimeProvider>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = CreateFixture(timeProviderMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.SuperAdmin)).Returns(false);
        fixture.MockCurrentUser.Setup(current => current.IsInRole(RoleNames.Administrator)).Returns(false);
        fixture.MockCurrentUser.Setup(current => current.GetRoles()).Returns(new[] { RoleNames.User });

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = userId, UserName = "target" });

        roleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationRole { Id = roleId, Name = RoleNames.User });

        var handler = fixture.CreateHandler();
        var command = new AssignRoleCommand(userId, roleId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("You do not have sufficient privileges to assign roles");

        fixture.VerifyStructuredErrorLogged(
            new Dictionary<string, object?>
            {
                ["UserId"] = adminId.ToString(),
                ["Roles"] = RoleNames.User
            },
            Times.Once());

        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Creates a fixture configured to construct the assign role handler with a time provider dependency.
    /// </summary>
    private static CommandHandlerTestFixture<AssignRoleCommandHandler> CreateFixture(Mock<ITimeProvider> timeProviderMock)
    {
        var fixture = new CommandHandlerTestFixture<AssignRoleCommandHandler>()
            .WithHandlerFactory((unitOfWork, currentUser, logger) =>
                new AssignRoleCommandHandler(unitOfWork, currentUser, timeProviderMock.Object, logger));

        return fixture;
    }
}

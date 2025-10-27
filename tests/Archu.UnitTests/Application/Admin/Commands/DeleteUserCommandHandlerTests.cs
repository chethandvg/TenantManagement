using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Commands.DeleteUser;
using Archu.Application.Common;
using Archu.Domain.Constants;
using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class DeleteUserCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenUserExists_ShouldDeleteSuccessfully(
        Guid userId,
        Guid adminId,
        string userName)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var user = new ApplicationUser { Id = userId, UserName = userName };

        var fixture = new CommandHandlerTestFixture<DeleteUserCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        CancellationToken capturedGetToken = default;
        CancellationToken capturedRoleToken = default;
        CancellationToken capturedDeleteToken = default;
        CancellationToken capturedSaveToken = default;

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedGetToken = token)
            .ReturnsAsync(user);

        roleRepositoryMock
            .Setup(repo => repo.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedRoleToken = token)
            .ReturnsAsync(new List<ApplicationRole>
            {
                new() { Id = Guid.NewGuid(), Name = RoleNames.User, NormalizedName = RoleNames.User.ToUpperInvariant() }
            });

        userRepositoryMock
            .Setup(repo => repo.DeleteAsync(user, It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, CancellationToken>((_, token) => capturedDeleteToken = token)
            .Returns(Task.CompletedTask);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedSaveToken = token)
            .ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        var handler = fixture.CreateHandler();
        var command = new DeleteUserCommand(userId);

        // Act
        var result = await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(Result.Success());

        userRepositoryMock.Verify(repo => repo.DeleteAsync(user, cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(cancellationTokenSource.Token), Times.Once());

        capturedGetToken.Should().Be(cancellationTokenSource.Token);
        capturedRoleToken.Should().Be(cancellationTokenSource.Token);
        capturedDeleteToken.Should().Be(cancellationTokenSource.Token);
        capturedSaveToken.Should().Be(cancellationTokenSource.Token);

        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["UserName"] = userName,
                ["UserId"] = userId,
                ["AdminUserId"] = adminId.ToString()
            },
            Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure(Guid userId, Guid adminId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = new CommandHandlerTestFixture<DeleteUserCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        CancellationToken capturedToken = default;
        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedToken = token)
            .ReturnsAsync((ApplicationUser?)null);

        var handler = fixture.CreateHandler();
        var command = new DeleteUserCommand(userId);

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
    public async Task Handle_WhenSelfDeletionAttempted_ShouldReturnFailure(Guid adminId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var fixture = new CommandHandlerTestFixture<DeleteUserCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);

        var handler = fixture.CreateHandler();
        var command = new DeleteUserCommand(adminId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("You cannot delete your own account");

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["AdminUserId"] = adminId.ToString()
            },
            Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenDeletingLastSuperAdmin_ShouldReturnFailure(
        Guid adminId,
        Guid targetUserId)
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        var user = new ApplicationUser { Id = targetUserId, UserName = "super" };
        var superAdminRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = RoleNames.SuperAdmin,
            NormalizedName = RoleNames.SuperAdmin.ToUpperInvariant()
        };

        var fixture = new CommandHandlerTestFixture<DeleteUserCommandHandler>()
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork.Setup(unit => unit.Users).Returns(userRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.Roles).Returns(roleRepositoryMock.Object);
        fixture.MockUnitOfWork.Setup(unit => unit.UserRoles).Returns(userRoleRepositoryMock.Object);

        CancellationToken capturedGetToken = default;
        CancellationToken capturedRolesToken = default;
        CancellationToken capturedRoleLookupToken = default;
        CancellationToken capturedCountToken = default;

        userRepositoryMock
            .Setup(repo => repo.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedGetToken = token)
            .ReturnsAsync(user);

        roleRepositoryMock
            .Setup(repo => repo.GetUserRolesAsync(targetUserId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedRolesToken = token)
            .ReturnsAsync(new List<ApplicationRole> { superAdminRole });

        roleRepositoryMock
            .Setup(repo => repo.GetByNameAsync(RoleNames.SuperAdmin, It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, token) => capturedRoleLookupToken = token)
            .ReturnsAsync(superAdminRole);

        userRoleRepositoryMock
            .Setup(repo => repo.CountUsersWithRoleAsync(superAdminRole.Id, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => capturedCountToken = token)
            .ReturnsAsync(1);

        var handler = fixture.CreateHandler();
        var command = new DeleteUserCommand(targetUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot delete the last SuperAdmin");

        capturedGetToken.Should().Be(CancellationToken.None);
        capturedRolesToken.Should().Be(CancellationToken.None);
        capturedRoleLookupToken.Should().Be(CancellationToken.None);
        capturedCountToken.Should().Be(CancellationToken.None);

        fixture.VerifyStructuredWarningLogged(
            new Dictionary<string, object?>
            {
                ["Reason"] = "Critical security restriction: Cannot delete the last SuperAdmin user from the system. At least one SuperAdmin must exist to maintain system administration capabilities. Please create or promote another SuperAdmin before deleting this user."
            },
            Times.Once());

        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        userRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

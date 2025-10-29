using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archu.Application.Abstractions.Authentication;
using Archu.Application.Abstractions.Repositories;
using Archu.Application.Admin.Commands.CreateUser;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Admin.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Admin")]
public class CreateUserCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIsCreated_ShouldPersistAndReturnDto(
        string userName,
        string email,
        string password,
        string phoneNumber,
        bool emailConfirmed,
        bool twoFactorEnabled,
        Guid adminId,
        Guid userId,
        byte[] rowVersion,
        DateTime createdAtUtc)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var hashedPassword = $"hashed-{password}";
        passwordHasherMock
            .Setup(hasher => hasher.HashPassword(password))
            .Returns(hashedPassword);

        var fixture = CreateFixture(passwordHasherMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork
            .Setup(unit => unit.Users)
            .Returns(userRepositoryMock.Object);

        CancellationToken capturedUserNameToken = default;
        CancellationToken capturedEmailToken = default;
        CancellationToken capturedAddToken = default;
        CancellationToken capturedSaveToken = default;
        ApplicationUser? capturedUser = null;

        userRepositoryMock
            .Setup(repo => repo.UserNameExistsAsync(
                userName,
                It.Is<Guid?>(value => value == null),
                It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, CancellationToken>((_, _, token) => capturedUserNameToken = token)
            .ReturnsAsync(false);

        userRepositoryMock
            .Setup(repo => repo.EmailExistsAsync(
                email,
                It.Is<Guid?>(value => value == null),
                It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, CancellationToken>((_, _, token) => capturedEmailToken = token)
            .ReturnsAsync(false);

        userRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, CancellationToken>((user, token) =>
            {
                capturedAddToken = token;
                capturedUser = user;
                user.Id = userId;
                user.RowVersion = rowVersion;
                user.CreatedAtUtc = createdAtUtc;
            })
            .ReturnsAsync((ApplicationUser user, CancellationToken _) => user);

        fixture.MockUnitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => capturedSaveToken = token)
            .ReturnsAsync(1);

        using var cancellationTokenSource = new CancellationTokenSource();

        var handler = fixture.CreateHandler();
        var command = new CreateUserCommand(
            userName,
            email,
            password,
            phoneNumber,
            emailConfirmed,
            twoFactorEnabled);

        // Act
        var result = await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.UserName.Should().Be(userName);
        result.Email.Should().Be(email);
        result.EmailConfirmed.Should().Be(emailConfirmed);
        result.TwoFactorEnabled.Should().Be(twoFactorEnabled);
        result.PhoneNumber.Should().Be(phoneNumber);
        result.RowVersion.Should().BeSameAs(rowVersion);
        result.CreatedAtUtc.Should().Be(createdAtUtc);
        result.Roles.Should().BeEmpty();

        capturedUser.Should().NotBeNull();
        capturedUser!.UserName.Should().Be(userName);
        capturedUser.Email.Should().Be(email);
        capturedUser.PasswordHash.Should().Be(hashedPassword);
        capturedUser.PhoneNumber.Should().Be(phoneNumber);
        capturedUser.EmailConfirmed.Should().Be(emailConfirmed);
        capturedUser.TwoFactorEnabled.Should().Be(twoFactorEnabled);

        userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ApplicationUser>(), cancellationTokenSource.Token), Times.Once());
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(cancellationTokenSource.Token), Times.Once());
        passwordHasherMock.Verify(hasher => hasher.HashPassword(password), Times.Once());

        capturedUserNameToken.Should().Be(cancellationTokenSource.Token);
        capturedEmailToken.Should().Be(cancellationTokenSource.Token);
        capturedAddToken.Should().Be(cancellationTokenSource.Token);
        capturedSaveToken.Should().Be(cancellationTokenSource.Token);

        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["UserId"] = adminId.ToString(),
                ["UserName"] = userName,
                ["{OriginalFormat}"] = "Admin {UserId} creating user: {UserName}"
            },
            Times.Once());

        fixture.VerifyStructuredInformationLogged(
            new Dictionary<string, object?>
            {
                ["{OriginalFormat}"] = "User created with ID: {UserId}"
            },
            Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserNameExists_ShouldThrowInvalidOperationException(
        string userName,
        string email,
        string password,
        Guid adminId)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var fixture = CreateFixture(passwordHasherMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork
            .Setup(unit => unit.Users)
            .Returns(userRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.UserNameExistsAsync(
                userName,
                It.Is<Guid?>(value => value == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = fixture.CreateHandler();
        var command = new CreateUserCommand(userName, email, password, null, false, false);

        // Act
        var action = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Username '{userName}' is already taken");

        fixture.VerifyWarningLogged("Username {UserName} already exists");
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        passwordHasherMock.Verify(hasher => hasher.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenEmailExists_ShouldThrowInvalidOperationException(
        string userName,
        string email,
        string password,
        Guid adminId)
    {
        // Arrange
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var userRepositoryMock = new Mock<IUserRepository>();

        var fixture = CreateFixture(passwordHasherMock)
            .WithAuthenticatedUser(adminId);

        fixture.MockUnitOfWork
            .Setup(unit => unit.Users)
            .Returns(userRepositoryMock.Object);

        userRepositoryMock
            .Setup(repo => repo.UserNameExistsAsync(
                userName,
                It.Is<Guid?>(value => value == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        userRepositoryMock
            .Setup(repo => repo.EmailExistsAsync(
                email,
                It.Is<Guid?>(value => value == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = fixture.CreateHandler();
        var command = new CreateUserCommand(userName, email, password, null, false, false);

        // Act
        var action = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Email '{email}' is already registered");

        fixture.VerifyWarningLogged("Email {Email} already exists");
        fixture.MockUnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        passwordHasherMock.Verify(hasher => hasher.HashPassword(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Creates a configured fixture instance for testing the create user handler.
    /// Ensures the handler is constructed with the password hasher dependency.
    /// </summary>
    private static CommandHandlerTestFixture<CreateUserCommandHandler> CreateFixture(Mock<IPasswordHasher> passwordHasherMock)
    {
        var fixture = new CommandHandlerTestFixture<CreateUserCommandHandler>()
            .WithHandlerFactory((unitOfWork, currentUser, logger) =>
                new CreateUserCommandHandler(unitOfWork, passwordHasherMock.Object, currentUser, logger));

        return fixture;
    }
}

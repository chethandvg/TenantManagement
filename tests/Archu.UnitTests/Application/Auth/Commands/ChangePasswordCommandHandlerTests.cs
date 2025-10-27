using Archu.Application.Auth.Commands.ChangePassword;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class ChangePasswordCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenRequestIncludesUserId_UsesProvidedIdentifier(
        string userId,
        string currentPassword,
        string newPassword)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ChangePasswordCommandHandler>()
            .WithAuthenticatedUser(Guid.NewGuid())
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.VerifyInformationLogged("Password changed successfully", Times.Once());
        fixture.MockAuthenticationService.Verify(
            service => service.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenServiceFails_ReturnsFailure(
        string userId,
        string currentPassword,
        string newPassword,
        string errorMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ChangePasswordCommandHandler>()
            .WithAuthenticatedUser(Guid.NewGuid())
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure(errorMessage)));

        var handler = fixture.CreateHandler();
        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        fixture.VerifyWarningLogged("Password change failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdMissing_UsesCurrentUser(
        Guid currentUserId,
        string currentPassword,
        string newPassword)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ChangePasswordCommandHandler>()
            .WithAuthenticatedUser(currentUserId)
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ChangePasswordAsync(currentUserId.ToString(), currentPassword, newPassword, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ChangePasswordCommand
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.MockAuthenticationService.Verify(
            service => service.ChangePasswordAsync(currentUserId.ToString(), currentPassword, newPassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenNoUserIdAvailable_ReturnsFailure(
        string currentPassword,
        string newPassword)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ChangePasswordCommandHandler>()
            .WithUnauthenticatedUser()
            .WithAuthenticationService();

        var handler = fixture.CreateHandler();
        var command = new ChangePasswordCommand
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not authenticated");
        fixture.VerifyWarningLogged("Password change attempt without valid user ID", Times.Once());
        fixture.MockAuthenticationService.Verify(
            service => service.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string userId,
        string currentPassword,
        string newPassword)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ChangePasswordCommandHandler>()
            .WithAuthenticatedUser(Guid.NewGuid())
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.ChangePasswordAsync(userId, currentPassword, newPassword, cancellationTokenSource.Token),
            Times.Once);
    }
}

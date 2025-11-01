using Archu.Application.Auth.Commands.Logout;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class LogoutCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdProvided_UsesRequestUserId(
        string userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<LogoutCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.LogoutAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()))
            .WithAuthenticatedUser(Guid.NewGuid());

        var handler = fixture.CreateHandler();
        var command = new LogoutCommand(userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.VerifyInformationLogged("User logged out successfully", Times.Once());
        fixture.MockAuthenticationService.Verify(
            service => service.LogoutAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenServiceFails_ReturnsFailure(
        string userId,
        string errorMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<LogoutCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.LogoutAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure(errorMessage)))
            .WithAuthenticatedUser(Guid.NewGuid());

        var handler = fixture.CreateHandler();
        var command = new LogoutCommand(userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        fixture.VerifyWarningLogged("Logout failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenUserIdMissingButCurrentUserAvailable_UsesCurrentUser(
        Guid currentUserId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<LogoutCommandHandler>()
            .WithAuthenticatedUser(currentUserId)
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.LogoutAsync(currentUserId.ToString(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new LogoutCommand(null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.MockAuthenticationService.Verify(
            service => service.LogoutAsync(currentUserId.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenNoUserIdAvailable_ReturnsFailure()
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<LogoutCommandHandler>()
            .WithUnauthenticatedUser()
            .WithAuthenticationService();

        var handler = fixture.CreateHandler();
        var command = new LogoutCommand(null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not authenticated");
        fixture.VerifyWarningLogged("Logout attempt without valid user ID", Times.Once());
        fixture.MockAuthenticationService.Verify(
            service => service.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string userId)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<LogoutCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.LogoutAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()))
            .WithAuthenticatedUser(Guid.NewGuid());

        var handler = fixture.CreateHandler();
        var command = new LogoutCommand(userId);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.LogoutAsync(userId, cancellationTokenSource.Token),
            Times.Once);
    }
}

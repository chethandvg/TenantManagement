using Archu.Application.Auth.Commands.ConfirmEmail;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class ConfirmEmailCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenConfirmationSucceeds_ReturnsSuccess(
        string userId,
        string token)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ConfirmEmailCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ConfirmEmailAsync(userId, token, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ConfirmEmailCommand(userId, token);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.VerifyInformationLogged("Email confirmed successfully", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenConfirmationFails_ReturnsFailure(
        string userId,
        string token,
        string errorMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ConfirmEmailCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ConfirmEmailAsync(userId, token, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure(errorMessage)));

        var handler = fixture.CreateHandler();
        var command = new ConfirmEmailCommand(userId, token);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        fixture.VerifyWarningLogged("Email confirmation failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string userId,
        string token)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ConfirmEmailCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ConfirmEmailAsync(userId, token, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ConfirmEmailCommand(userId, token);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.ConfirmEmailAsync(userId, token, cancellationTokenSource.Token),
            Times.Once);
    }
}

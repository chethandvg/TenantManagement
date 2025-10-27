using Archu.Application.Auth.Commands.ResetPassword;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class ResetPasswordCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenResetSucceeds_ReturnsSuccess(
        string email,
        string token,
        string newPassword)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ResetPasswordCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ResetPasswordAsync(email, token, newPassword, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ResetPasswordCommand(email, token, newPassword);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.VerifyInformationLogged("Password reset successfully", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenResetFails_ReturnsFailure(
        string email,
        string token,
        string newPassword,
        string errorMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ResetPasswordCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ResetPasswordAsync(email, token, newPassword, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure(errorMessage)));

        var handler = fixture.CreateHandler();
        var command = new ResetPasswordCommand(email, token, newPassword);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        fixture.VerifyWarningLogged("Password reset failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string email,
        string token,
        string newPassword)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ResetPasswordCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ResetPasswordAsync(email, token, newPassword, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ResetPasswordCommand(email, token, newPassword);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.ResetPasswordAsync(email, token, newPassword, cancellationTokenSource.Token),
            Times.Once);
    }
}

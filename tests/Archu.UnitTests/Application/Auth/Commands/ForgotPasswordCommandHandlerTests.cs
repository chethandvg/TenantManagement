using System.Collections.Generic;
using Archu.Application.Auth.Commands.ForgotPassword;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class ForgotPasswordCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenServiceSucceeds_ReturnsSuccess(
        string email)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ForgotPasswordCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ForgotPasswordAsync(email, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ForgotPasswordCommand(email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "Email", email },
            { "{OriginalFormat}", "Password reset email processed successfully for: {Email}" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenServiceFails_ReturnsGenericFailure(
        string email,
        string errorMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ForgotPasswordCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ForgotPasswordAsync(email, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure(errorMessage)));

        var handler = fixture.CreateHandler();
        var command = new ForgotPasswordCommand(email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Unable to process password reset request at this time. Please try again later.");
        fixture.VerifyStructuredErrorLogged(new Dictionary<string, object?>
        {
            { "Email", email },
            { "Error", errorMessage },
            { "{OriginalFormat}", "Password reset failed for email: {Email}. Error: {Error}" }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string email)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<ForgotPasswordCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ForgotPasswordAsync(email, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success()));

        var handler = fixture.CreateHandler();
        var command = new ForgotPasswordCommand(email);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.ForgotPasswordAsync(email, cancellationTokenSource.Token),
            Times.Once);
    }
}

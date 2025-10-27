using Archu.Application.Abstractions.Authentication;
using Archu.Application.Auth.Commands.RefreshToken;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class RefreshTokenCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenRefreshTokenValid_ReturnsAuthenticationResult(
        string refreshToken,
        AuthenticationResult authResult)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<RefreshTokenCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.RefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Success(authResult)));

        var handler = fixture.CreateHandler();
        var command = new RefreshTokenCommand(refreshToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(authResult);
        fixture.VerifyInformationLogged("Access token refreshed successfully", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRefreshTokenInvalid_ReturnsFailure(
        string refreshToken,
        string errorMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<RefreshTokenCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.RefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Failure(errorMessage)));

        var handler = fixture.CreateHandler();
        var command = new RefreshTokenCommand(refreshToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired refresh token");
        fixture.VerifyWarningLogged("Token refresh failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string refreshToken,
        AuthenticationResult authResult)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<RefreshTokenCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.RefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Success(authResult)));

        var handler = fixture.CreateHandler();
        var command = new RefreshTokenCommand(refreshToken);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.RefreshTokenAsync(refreshToken, cancellationTokenSource.Token),
            Times.Once);
    }
}

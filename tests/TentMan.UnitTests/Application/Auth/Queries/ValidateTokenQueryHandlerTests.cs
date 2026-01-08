using TentMan.Application.Auth.Queries.ValidateToken;
using TentMan.Application.Common;
using TentMan.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace TentMan.UnitTests.Application.Auth.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class ValidateTokenQueryHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenTokenValid_ReturnsSuccess(
        string refreshToken)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<ValidateTokenQueryHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ValidateRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<bool>.Success(true)));

        var handler = fixture.CreateHandler();
        var query = new ValidateTokenQuery(refreshToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        fixture.VerifyInformationLogged("Token validation result", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenTokenInvalid_ReturnsFailure(
        string refreshToken,
        string errorMessage)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<ValidateTokenQueryHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ValidateRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<bool>.Failure(errorMessage)));

        var handler = fixture.CreateHandler();
        var query = new ValidateTokenQuery(refreshToken);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        fixture.VerifyWarningLogged("Token validation failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string refreshToken)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<ValidateTokenQueryHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.ValidateRefreshTokenAsync(refreshToken, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<bool>.Success(true)));

        var handler = fixture.CreateHandler();
        var query = new ValidateTokenQuery(refreshToken);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(query, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.ValidateRefreshTokenAsync(refreshToken, cancellationTokenSource.Token),
            Times.Once);
    }
}

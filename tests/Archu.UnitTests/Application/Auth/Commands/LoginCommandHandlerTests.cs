using Archu.Application.Abstractions.Authentication;
using Archu.Application.Auth.Commands.Login;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class LoginCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenCredentialsAreValid_ReturnsAuthenticationResult(
        string email,
        string password,
        Guid userId)
    {
        // Arrange
        var authResult = new AuthenticationResult
        {
            AccessToken = Guid.NewGuid().ToString(),
            RefreshToken = Guid.NewGuid().ToString(),
            User = new UserInfo
            {
                Id = userId.ToString(),
                Email = email
            }
        };

        var fixture = new CommandHandlerTestFixture<LoginCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.LoginAsync(email, password, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Success(authResult)));

        var handler = fixture.CreateHandler();
        var command = new LoginCommand
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().BeSameAs(authResult);
        fixture.VerifyInformationLogged("User logged in successfully", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenServiceReturnsFailure_ReturnsGenericError(
        string email,
        string password,
        string failureMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<LoginCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.LoginAsync(email, password, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Failure(failureMessage)));

        var handler = fixture.CreateHandler();
        var command = new LoginCommand
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password");
        fixture.VerifyWarningLogged("Login failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_PropagatesCancellationTokenToService(
        string email,
        string password,
        AuthenticationResult authResult)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<LoginCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.LoginAsync(email, password, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Success(authResult)));

        var handler = fixture.CreateHandler();
        var command = new LoginCommand
        {
            Email = email,
            Password = password
        };
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.LoginAsync(email, password, cancellationTokenSource.Token),
            Times.Once);
    }
}

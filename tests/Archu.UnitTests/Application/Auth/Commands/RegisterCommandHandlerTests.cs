using Archu.Application.Abstractions.Authentication;
using Archu.Application.Auth.Commands.Register;
using Archu.Application.Common;
using Archu.UnitTests.TestHelpers.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Auth.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "Auth")]
public class RegisterCommandHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_WhenRegistrationSucceeds_ReturnsAuthenticationResult(
        string email,
        string password,
        string userName,
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
                Email = email,
                UserName = userName
            }
        };

        var fixture = new CommandHandlerTestFixture<RegisterCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.RegisterAsync(email, password, userName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Success(authResult)));

        var handler = fixture.CreateHandler();
        var command = new RegisterCommand(email, password, userName);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().BeSameAs(authResult);
        fixture.VerifyInformationLogged("User registered successfully", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_WhenRegistrationFails_ReturnsFailureFromService(
        string email,
        string password,
        string userName,
        string errorMessage)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<RegisterCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.RegisterAsync(email, password, userName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Failure(errorMessage)));

        var handler = fixture.CreateHandler();
        var command = new RegisterCommand(email, password, userName);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        fixture.VerifyWarningLogged("Registration failed", Times.Once());
    }

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToService(
        string email,
        string password,
        string userName,
        AuthenticationResult authResult)
    {
        // Arrange
        var fixture = new CommandHandlerTestFixture<RegisterCommandHandler>()
            .WithAuthenticationService(mock =>
                mock.Setup(service => service.RegisterAsync(email, password, userName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<AuthenticationResult>.Success(authResult)));

        var handler = fixture.CreateHandler();
        var command = new RegisterCommand(email, password, userName);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        fixture.MockAuthenticationService.Verify(
            service => service.RegisterAsync(email, password, userName, cancellationTokenSource.Token),
            Times.Once);
    }
}

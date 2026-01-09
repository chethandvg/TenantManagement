using System.Net;
using System.Text.Json;
using TentMan.ApiClient.Exceptions;
using TentMan.ApiClient.Services;
using TentMan.Contracts.Authentication;
using TentMan.Contracts.Common;
using TentMan.Contracts.TenantInvites;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace TentMan.ApiClient.Tests.Services;

public sealed class TenantInvitesApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<TenantInvitesApiClient>> _mockLogger;
    private readonly TenantInvitesApiClient _apiClient;

    public TenantInvitesApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com");
        
        _mockLogger = new Mock<ILogger<TenantInvitesApiClient>>();
        _apiClient = new TenantInvitesApiClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnSuccess_WhenTokenIsValid()
    {
        // Arrange
        var token = "a1b2c3d4e5f67890abcdef1234567890";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = true,
            TenantFullName = "John Doe",
            Phone = "+1234567890",
            Email = "john.doe@example.com",
            ErrorMessage = null
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse, "Invite validated");

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={token}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeTrue();
        result.Data.TenantFullName.Should().Be("John Doe");
        result.Data.Email.Should().Be("john.doe@example.com");
        result.Data.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnInvalid_WhenTokenIsExpired()
    {
        // Arrange
        var token = "expiredtoken123456789012345678";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = false,
            TenantFullName = null,
            Phone = null,
            Email = null,
            ErrorMessage = "This invite has expired"
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse, "Invite validated");

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={token}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeFalse();
        result.Data.ErrorMessage.Should().Be("This invite has expired");
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldReturnInvalid_WhenTokenIsUsed()
    {
        // Arrange
        var token = "usedtoken12345678901234567890";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = false,
            TenantFullName = null,
            Phone = null,
            Email = null,
            ErrorMessage = "Invite has already been used"
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse, "Invite validated");

        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token={token}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data!.IsValid.Should().BeFalse();
        result.Data.ErrorMessage.Should().Be("Invite has already been used");
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldThrowArgumentNullException_WhenTokenIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.ValidateInviteAsync(null!));
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldThrowArgumentException_WhenTokenIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _apiClient.ValidateInviteAsync(string.Empty));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldReturnSuccess_WhenInviteIsAccepted()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        var authResponse = new AuthenticationResponse
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            RefreshToken = "abc123def456...",
            ExpiresIn = 3600,
            TokenType = "Bearer",
            User = new UserInfoDto
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "johndoe",
                Email = "john.doe@example.com",
                EmailConfirmed = true,
                Roles = new[] { "Tenant" }
            }
        };

        var apiResponse = ApiResponse<AuthenticationResponse>.Ok(authResponse, "Invite accepted and user created successfully");

        _mockHttp
            .When("https://api.test.com/api/v1/invites/accept")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.AcceptInviteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Data.User.UserName.Should().Be("johndoe");
        result.Data.User.Email.Should().Be("john.doe@example.com");
        result.Data.User.Roles.Should().Contain("Tenant");
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldReturnFail_WhenEmailMismatch()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "johndoe",
            Email = "wrong.email@example.com",
            Password = "SecurePass123!@#"
        };

        var apiResponse = ApiResponse<object>.Fail("Email does not match the invite");

        _mockHttp
            .When("https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", JsonSerializer.Serialize(apiResponse));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldReturnFail_WhenUsernameIsTaken()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "existinguser",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        var apiResponse = ApiResponse<object>.Fail("Username is already taken");

        _mockHttp
            .When("https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", JsonSerializer.Serialize(apiResponse));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldReturnFail_WhenInviteIsExpired()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "expiredtoken123456789012345678",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        var apiResponse = ApiResponse<object>.Fail("Invite has expired");

        _mockHttp
            .When("https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", JsonSerializer.Serialize(apiResponse));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldReturnFail_WhenInviteIsAlreadyUsed()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "usedtoken12345678901234567890",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        var apiResponse = ApiResponse<object>.Fail("Invite has already been used");

        _mockHttp
            .When("https://api.test.com/api/v1/invites/accept")
            .Respond(HttpStatusCode.BadRequest, "application/json", JsonSerializer.Serialize(apiResponse));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AcceptInviteAsync(request));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.AcceptInviteAsync(null!));
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldHandleRequestCancellation()
    {
        // Arrange
        var request = new AcceptInviteRequest
        {
            InviteToken = "a1b2c3d4e5f67890abcdef1234567890",
            UserName = "johndoe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!@#"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp
            .When("https://api.test.com/api/v1/invites/accept")
            .Respond(async () =>
            {
                await Task.Delay(1000);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _apiClient.AcceptInviteAsync(request, cts.Token);

        // Assert - Should return failed response for cancelled request
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cancelled");
    }

    [Fact]
    public async Task ValidateInviteAsync_ShouldEscapeTokenInUrl()
    {
        // Arrange
        var token = "token+with/special=chars";
        var validateResponse = new ValidateInviteResponse
        {
            IsValid = true,
            TenantFullName = "John Doe",
            Phone = "+1234567890",
            Email = null,
            ErrorMessage = null
        };

        var apiResponse = ApiResponse<ValidateInviteResponse>.Ok(validateResponse);

        // The token should be URL-encoded
        _mockHttp
            .When($"https://api.test.com/api/v1/invites/validate?token=token%2Bwith%2Fspecial%3Dchars")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.ValidateInviteAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        _mockHttp.VerifyNoOutstandingExpectation();
    }
}

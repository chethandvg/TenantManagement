using System.Net;
using System.Text.Json;
using TentMan.AdminApiClient.Configuration;
using TentMan.AdminApiClient.Exceptions;
using TentMan.AdminApiClient.Services;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace TentMan.AdminApiClient.Tests.Services;

/// <summary>
/// Unit tests for the InitializationApiClient class.
/// </summary>
public class InitializationApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<InitializationApiClient>> _mockLogger;
    private readonly InitializationApiClient _apiClient;

    public InitializationApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com");

        var options = Options.Create(new AdminApiClientOptions { ApiVersion = "v1" });
        _mockLogger = new Mock<ILogger<InitializationApiClient>>();
        _apiClient = new InitializationApiClient(_httpClient, options, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldReturnSuccess_WhenSystemInitialized()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "superadmin",
            Email = "admin@example.com",
            Password = "SecurePassword123!"
        };

        var result = new InitializationResultDto
        {
            RolesCreated = true,
            RolesCount = 5,
            UserCreated = true,
            UserId = Guid.NewGuid(),
            Message = "System initialized successfully"
        };

        var apiResponse = ApiResponse<InitializationResultDto>.Ok(result, "System initialized successfully");

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var response = await _apiClient.InitializeSystemAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.RolesCreated.Should().BeTrue();
        response.Data.RolesCount.Should().Be(5);
        response.Data.UserCreated.Should().BeTrue();
        response.Data.UserId.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowValidationException_WhenSystemAlreadyInitialized()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "superadmin",
            Email = "admin@example.com",
            Password = "SecurePassword123!"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"success\":false,\"message\":\"System is already initialized\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.InitializeSystemAsync(request));
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowValidationException_WhenInvalidRequest()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "",
            Email = "invalid-email",
            Password = "weak"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"success\":false,\"message\":\"Validation failed\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.InitializeSystemAsync(request));
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.InitializeSystemAsync(null!));
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowServerException_When500()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "superadmin",
            Email = "admin@example.com",
            Password = "SecurePassword123!"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"success\":false,\"message\":\"Server error during initialization\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ServerException>(
            async () => await _apiClient.InitializeSystemAsync(request));
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldHandleRequestCancellation()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "superadmin",
            Email = "admin@example.com",
            Password = "SecurePassword123!"
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(async () =>
            {
                await Task.Delay(1000);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _apiClient.InitializeSystemAsync(request, cts.Token);

        // Assert - Should return failed response for cancelled request
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cancelled");
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldReturnCorrectResultDetails()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "admin",
            Email = "admin@test.com",
            Password = "StrongP@ssword1!"
        };

        var expectedUserId = Guid.NewGuid();
        var result = new InitializationResultDto
        {
            RolesCreated = true,
            RolesCount = 5,
            UserCreated = true,
            UserId = expectedUserId,
            Message = "System initialized with 5 roles and super admin user"
        };

        var apiResponse = ApiResponse<InitializationResultDto>.Ok(result);

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var response = await _apiClient.InitializeSystemAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.RolesCreated.Should().BeTrue();
        response.Data.RolesCount.Should().Be(5);
        response.Data.UserCreated.Should().BeTrue();
        response.Data.UserId.Should().Be(expectedUserId);
        response.Data.Message.Should().Contain("5 roles");
    }
}

using System.Net;
using System.Text.Json;
using TentMan.AdminApiClient.Services;
using TentMan.ApiClient.Exceptions;
using TentMan.Application.Admin.Commands.InitializeSystem;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace TentMan.AdminApiClient.Tests.Services;

/// <summary>
/// Unit tests for <see cref="InitializationApiClient"/>.
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
        
        _mockLogger = new Mock<ILogger<InitializationApiClient>>();
        _apiClient = new InitializationApiClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldReturnSuccess_WhenSystemIsInitialized()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "superadmin",
            Email = "admin@test.com",
            Password = "SecurePassword123!"
        };

        var initResult = new InitializationResult(
            RolesCreated: true,
            RolesCount: 5,
            UserCreated: true,
            UserId: Guid.NewGuid(),
            OrganizationCreated: false,
            OrganizationId: null,
            OwnerCreated: false,
            OwnerId: null,
            Message: "System initialized successfully"
        );

        var apiResponse = ApiResponse<InitializationResult>.Ok(
            initResult,
            "System initialized successfully");

        _mockHttp
            .When("https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.InitializeSystemAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolesCreated.Should().BeTrue();
        result.Data.RolesCount.Should().Be(5);
        result.Data.UserCreated.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.InitializeSystemAsync(null!));
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowValidationException_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "",
            Email = "invalid-email",
            Password = "weak"
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Validation failed\",\"errors\":[\"UserName is required\"]}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.InitializeSystemAsync(request));
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowApiClientException_WhenSystemAlreadyInitialized()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "superadmin",
            Email = "admin@test.com",
            Password = "SecurePassword123!"
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"System is already initialized\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.InitializeSystemAsync(request));
    }

    [Fact]
    public async Task InitializeSystemAsync_ShouldThrowServerException_When500()
    {
        // Arrange
        var request = new InitializeSystemRequest
        {
            UserName = "superadmin",
            Email = "admin@test.com",
            Password = "SecurePassword123!"
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(HttpStatusCode.InternalServerError, "application/json",
                "{\"success\":false,\"message\":\"Server error\"}");

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
            Email = "admin@test.com",
            Password = "SecurePassword123!"
        };

        using var cts = new CancellationTokenSource();
        
        _mockHttp
            .When("https://api.test.com/api/v1/admin/initialization/initialize")
            .Respond(async (req) =>
            {
                cts.Cancel(); // Cancel during the request
                await Task.Delay(100, CancellationToken.None);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _apiClient.InitializeSystemAsync(request, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cancelled");
    }
}

using System.Net;
using System.Text.Json;
using TentMan.AdminApiClient.Exceptions;
using TentMan.AdminApiClient.Services;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace TentMan.AdminApiClient.Tests.Services;

/// <summary>
/// Unit tests for the UsersApiClient class.
/// </summary>
public class UsersApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<UsersApiClient>> _mockLogger;
    private readonly UsersApiClient _apiClient;

    public UsersApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com");

        _mockLogger = new Mock<ILogger<UsersApiClient>>();
        _apiClient = new UsersApiClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnSuccess_WhenApiReturnsUsers()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new() { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@example.com" },
            new() { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@example.com" }
        };

        var apiResponse = ApiResponse<IEnumerable<UserDto>>.Ok(users);

        _mockHttp
            .When("https://api.test.com/api/v1/admin/users?pageNumber=1&pageSize=10")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetUsersAsync(pageNumber: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldUseDefaultPagination_WhenNoParametersProvided()
    {
        // Arrange
        var apiResponse = ApiResponse<IEnumerable<UserDto>>.Ok(new List<UserDto>());

        _mockHttp
            .When("https://api.test.com/api/v1/admin/users?pageNumber=1&pageSize=10")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetUsersAsync();

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(5, 50)]
    public async Task GetUsersAsync_ShouldSendCorrectQueryParameters(int pageNumber, int pageSize)
    {
        // Arrange
        var apiResponse = ApiResponse<IEnumerable<UserDto>>.Ok(new List<UserDto>());

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/users?pageNumber={pageNumber}&pageSize={pageSize}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetUsersAsync(pageNumber, pageSize);

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldThrowResourceNotFoundException_When404()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/users?pageNumber=1&pageSize=10")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"success\":false,\"message\":\"Not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            async () => await _apiClient.GetUsersAsync());
    }

    [Fact]
    public async Task GetUsersAsync_ShouldThrowAuthorizationException_When401()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/users?pageNumber=1&pageSize=10")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"success\":false,\"message\":\"Unauthorized\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.GetUsersAsync());
    }

    [Fact]
    public async Task GetUsersAsync_ShouldThrowServerException_When500()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/users?pageNumber=1&pageSize=10")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"success\":false,\"message\":\"Server error\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ServerException>(
            async () => await _apiClient.GetUsersAsync());
    }

    [Fact]
    public async Task GetUsersAsync_ShouldHandleRequestCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp
            .When("https://api.test.com/api/v1/admin/users?pageNumber=1&pageSize=10")
            .Respond(async () =>
            {
                await Task.Delay(1000);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _apiClient.GetUsersAsync(cancellationToken: cts.Token);

        // Assert - Should return failed response for cancelled request
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cancelled");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnCreatedUser_WhenSuccessful()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            UserName = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!"
        };

        var createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email
        };

        var apiResponse = ApiResponse<UserDto>.Ok(createdUser, "User created successfully");

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/users")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be(request.UserName);
        result.Data.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowValidationException_When400()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            UserName = "newuser",
            Email = "invalid-email",
            Password = "weak"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/users")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"success\":false,\"message\":\"Validation failed\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CreateUserAsync(request));
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.CreateUserAsync(null!));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnSuccess_WhenUserDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/admin/users/{userId}")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _apiClient.DeleteUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldThrowResourceNotFoundException_When404()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/admin/users/{userId}")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"success\":false,\"message\":\"User not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            async () => await _apiClient.DeleteUserAsync(userId));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldThrowAuthorizationException_When403()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/admin/users/{userId}")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"success\":false,\"message\":\"Access denied\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.DeleteUserAsync(userId));
    }
}

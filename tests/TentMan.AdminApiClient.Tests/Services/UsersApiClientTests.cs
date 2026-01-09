using System.Net;
using System.Text.Json;
using TentMan.AdminApiClient.Services;
using TentMan.ApiClient.Exceptions;
using TentMan.Contracts.Admin;
using TentMan.Contracts.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace TentMan.AdminApiClient.Tests.Services;

/// <summary>
/// Unit tests for <see cref="UsersApiClient"/>.
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
    public async Task GetUsersAsync_ShouldReturnSuccess_WhenUsersExist()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new() { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@test.com", Roles = new List<string> { "User" } },
            new() { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@test.com", Roles = new List<string> { "Manager" } }
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
        result.Data.Should().HaveCount(2);
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
        await _apiClient.GetUsersAsync(pageNumber, pageSize);

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
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
        await _apiClient.GetUsersAsync();

        // Assert
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldThrowAuthorizationException_When401()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/users?pageNumber=1&pageSize=10")
            .Respond(HttpStatusCode.Unauthorized, "application/json",
                "{\"success\":false,\"message\":\"Unauthorized\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.GetUsersAsync());
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnSuccess_WhenUserIsCreated()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            UserName = "john.doe",
            Email = "john.doe@test.com",
            Password = "SecurePassword123!",
            PhoneNumber = "+1234567890",
            EmailConfirmed = true,
            TwoFactorEnabled = false
        };

        var userDto = new UserDto
        {
            Id = Guid.NewGuid(),
            UserName = "john.doe",
            Email = "john.doe@test.com",
            EmailConfirmed = true,
            PhoneNumber = "+1234567890",
            Roles = new List<string>()
        };

        var apiResponse = ApiResponse<UserDto>.Ok(userDto, "User created successfully");

        _mockHttp
            .When("https://api.test.com/api/v1/admin/users")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be("john.doe");
        result.Data.Email.Should().Be("john.doe@test.com");
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.CreateUserAsync(null!));
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowValidationException_WhenEmailIsTaken()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            UserName = "john.doe",
            Email = "existing@test.com",
            Password = "SecurePassword123!"
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/users")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Email already exists\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CreateUserAsync(request));
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrowValidationException_WhenPasswordIsWeak()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            UserName = "john.doe",
            Email = "john.doe@test.com",
            Password = "weak"
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/users")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Validation failed\",\"errors\":[\"Password is too weak\"]}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CreateUserAsync(request));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnSuccess_WhenUserIsDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var apiResponse = ApiResponse<bool>.Ok(true, "User deleted successfully");

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/users/{userId}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

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
            .When($"https://api.test.com/api/v1/admin/users/{userId}")
            .Respond(HttpStatusCode.NotFound, "application/json",
                "{\"success\":false,\"message\":\"User not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            async () => await _apiClient.DeleteUserAsync(userId));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldThrowValidationException_WhenDeletingLastSuperAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/users/{userId}")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Cannot delete the last SuperAdmin\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.DeleteUserAsync(userId));
    }
}

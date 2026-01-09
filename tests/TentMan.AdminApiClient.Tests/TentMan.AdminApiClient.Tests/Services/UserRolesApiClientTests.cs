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
/// Unit tests for the UserRolesApiClient class.
/// </summary>
public class UserRolesApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<UserRolesApiClient>> _mockLogger;
    private readonly UserRolesApiClient _apiClient;

    public UserRolesApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com");

        var options = Options.Create(new AdminApiClientOptions { ApiVersion = "v1" });
        _mockLogger = new Mock<ILogger<UserRolesApiClient>>();
        _apiClient = new UserRolesApiClient(_httpClient, options, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnSuccess_WhenApiReturnsRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roles = new List<RoleDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Manager", NormalizedName = "MANAGER" },
            new() { Id = Guid.NewGuid(), Name = "User", NormalizedName = "USER" }
        };

        var apiResponse = ApiResponse<IEnumerable<RoleDto>>.Ok(roles);

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetUserRolesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldHandleEmptyRolesList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var apiResponse = ApiResponse<IEnumerable<RoleDto>>.Ok(new List<RoleDto>());

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetUserRolesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldThrowResourceNotFoundException_When404()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"success\":false,\"message\":\"User not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            async () => await _apiClient.GetUserRolesAsync(userId));
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldThrowAuthorizationException_When403()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"success\":false,\"message\":\"Access denied\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.GetUserRolesAsync(userId));
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldReturnSuccess_WhenRoleAssigned()
    {
        // Arrange
        var request = new AssignRoleRequest
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var apiResponse = ApiResponse<object>.Ok(new { }, "Role assigned successfully");

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/userroles/assign")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.AssignRoleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldThrowValidationException_WhenUserAlreadyHasRole()
    {
        // Arrange
        var request = new AssignRoleRequest
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/userroles/assign")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"success\":false,\"message\":\"User already has the role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AssignRoleAsync(request));
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldThrowAuthorizationException_When403()
    {
        // Arrange
        var request = new AssignRoleRequest
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/userroles/assign")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"success\":false,\"message\":\"Only SuperAdmin can assign this role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.AssignRoleAsync(request));
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.AssignRoleAsync(null!));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldReturnSuccess_WhenRoleRemoved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _apiClient.RemoveRoleAsync(userId, roleId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldThrowValidationException_WhenCannotRemoveRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"success\":false,\"message\":\"Cannot remove the last SuperAdmin role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.RemoveRoleAsync(userId, roleId));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldThrowAuthorizationException_When403()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"success\":false,\"message\":\"Only SuperAdmin can remove this role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.RemoveRoleAsync(userId, roleId));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldThrowResourceNotFoundException_WhenUserOrRoleNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When(HttpMethod.Delete, $"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"success\":false,\"message\":\"User or role not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            async () => await _apiClient.RemoveRoleAsync(userId, roleId));
    }
}

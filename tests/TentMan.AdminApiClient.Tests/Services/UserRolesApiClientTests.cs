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
/// Unit tests for <see cref="UserRolesApiClient"/>.
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
        
        _mockLogger = new Mock<ILogger<UserRolesApiClient>>();
        _apiClient = new UserRolesApiClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnSuccess_WhenUserHasRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roles = new List<RoleDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Manager", NormalizedName = "MANAGER", Description = "Manager role" },
            new() { Id = Guid.NewGuid(), Name = "User", NormalizedName = "USER", Description = "User role" }
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
        result.Data.Should().HaveCount(2);
        result.Data.Should().Contain(r => r.Name == "Manager");
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnEmptyList_WhenUserHasNoRoles()
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
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldThrowResourceNotFoundException_When404()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}")
            .Respond(HttpStatusCode.NotFound, "application/json",
                "{\"success\":false,\"message\":\"User not found\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            async () => await _apiClient.GetUserRolesAsync(userId));
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldReturnSuccess_WhenRoleIsAssigned()
    {
        // Arrange
        var request = new AssignRoleRequest
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var apiResponse = ApiResponse<object>.Ok(new { }, "Role assigned successfully");

        _mockHttp
            .When("https://api.test.com/api/v1/admin/userroles/assign")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.AssignRoleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("assigned");
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.AssignRoleAsync(null!));
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
            .When("https://api.test.com/api/v1/admin/userroles/assign")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"User already has this role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.AssignRoleAsync(request));
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldThrowAuthorizationException_WhenNotAuthorized()
    {
        // Arrange
        var request = new AssignRoleRequest
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/userroles/assign")
            .Respond(HttpStatusCode.Forbidden, "application/json",
                "{\"success\":false,\"message\":\"Only SuperAdmin can assign SuperAdmin role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.AssignRoleAsync(request));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldReturnSuccess_WhenRoleIsRemoved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var apiResponse = ApiResponse<bool>.Ok(true, "Role removed successfully");

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.RemoveRoleAsync(userId, roleId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldThrowValidationException_WhenUserDoesNotHaveRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"User does not have this role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.RemoveRoleAsync(userId, roleId));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldThrowValidationException_WhenRemovingLastSuperAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Cannot remove the last SuperAdmin role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.RemoveRoleAsync(userId, roleId));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldThrowValidationException_WhenRemovingOwnSuperAdminRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Cannot remove your own SuperAdmin role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.RemoveRoleAsync(userId, roleId));
    }

    [Fact]
    public async Task RemoveRoleAsync_ShouldThrowAuthorizationException_WhenNotAuthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _mockHttp
            .When($"https://api.test.com/api/v1/admin/userroles/{userId}/roles/{roleId}")
            .Respond(HttpStatusCode.Forbidden, "application/json",
                "{\"success\":false,\"message\":\"Only SuperAdmin can remove SuperAdmin role\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.RemoveRoleAsync(userId, roleId));
    }
}

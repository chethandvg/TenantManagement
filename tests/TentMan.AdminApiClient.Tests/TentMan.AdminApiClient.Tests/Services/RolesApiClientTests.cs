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
/// Unit tests for the RolesApiClient class.
/// </summary>
public class RolesApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<RolesApiClient>> _mockLogger;
    private readonly RolesApiClient _apiClient;

    public RolesApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("https://api.test.com");

        var options = Options.Create(new AdminApiClientOptions { ApiVersion = "v1" });
        _mockLogger = new Mock<ILogger<RolesApiClient>>();
        _apiClient = new RolesApiClient(_httpClient, options, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnSuccess_WhenApiReturnsRoles()
    {
        // Arrange
        var roles = new List<RoleDto>
        {
            new() { Id = Guid.NewGuid(), Name = "SuperAdmin", NormalizedName = "SUPERADMIN" },
            new() { Id = Guid.NewGuid(), Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
            new() { Id = Guid.NewGuid(), Name = "User", NormalizedName = "USER" }
        };

        var apiResponse = ApiResponse<IEnumerable<RoleDto>>.Ok(roles);

        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetRolesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRolesAsync_ShouldHandleEmptyRolesList()
    {
        // Arrange
        var apiResponse = ApiResponse<IEnumerable<RoleDto>>.Ok(new List<RoleDto>());

        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.GetRolesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_ShouldThrowAuthorizationException_When401()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"success\":false,\"message\":\"Unauthorized\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.GetRolesAsync());
    }

    [Fact]
    public async Task GetRolesAsync_ShouldThrowAuthorizationException_When403()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"success\":false,\"message\":\"Access denied\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.GetRolesAsync());
    }

    [Fact]
    public async Task GetRolesAsync_ShouldThrowServerException_When500()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"success\":false,\"message\":\"Server error\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ServerException>(
            async () => await _apiClient.GetRolesAsync());
    }

    [Fact]
    public async Task GetRolesAsync_ShouldHandleRequestCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(async () =>
            {
                await Task.Delay(1000);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _apiClient.GetRolesAsync(cancellationToken: cts.Token);

        // Assert - Should return failed response for cancelled request
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cancelled");
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnCreatedRole_WhenSuccessful()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "ContentEditor",
            Description = "Can edit content but not manage users"
        };

        var createdRole = new RoleDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            NormalizedName = request.Name.ToUpperInvariant(),
            Description = request.Description
        };

        var apiResponse = ApiResponse<RoleDto>.Ok(createdRole, "Role created successfully");

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/roles")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.CreateRoleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(request.Name);
        result.Data.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldThrowValidationException_When400()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "SuperAdmin", // Duplicate name
            Description = "Trying to create duplicate role"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{\"success\":false,\"message\":\"Role already exists\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CreateRoleAsync(request));
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.CreateRoleAsync(null!));
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldThrowAuthorizationException_When403()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "NewRole"
        };

        _mockHttp
            .When(HttpMethod.Post, "https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{\"success\":false,\"message\":\"Access denied\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.CreateRoleAsync(request));
    }
}

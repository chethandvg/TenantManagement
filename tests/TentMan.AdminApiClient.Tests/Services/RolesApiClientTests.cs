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
/// Unit tests for <see cref="RolesApiClient"/>.
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
        
        _mockLogger = new Mock<ILogger<RolesApiClient>>();
        _apiClient = new RolesApiClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnSuccess_WhenRolesExist()
    {
        // Arrange
        var roles = new List<RoleDto>
        {
            new() { Id = Guid.NewGuid(), Name = "SuperAdmin", NormalizedName = "SUPERADMIN", Description = "Super Administrator" },
            new() { Id = Guid.NewGuid(), Name = "Administrator", NormalizedName = "ADMINISTRATOR", Description = "Administrator" },
            new() { Id = Guid.NewGuid(), Name = "Manager", NormalizedName = "MANAGER", Description = "Manager" }
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
        result.Data.Should().HaveCount(3);
        result.Data.Should().Contain(r => r.Name == "SuperAdmin");
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnEmptyList_WhenNoRolesExist()
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
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_ShouldThrowAuthorizationException_When401()
    {
        // Arrange
        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.Unauthorized, "application/json",
                "{\"success\":false,\"message\":\"Unauthorized\"}");

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
            .Respond(HttpStatusCode.Forbidden, "application/json",
                "{\"success\":false,\"message\":\"Forbidden\"}");

        // Act & Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            async () => await _apiClient.GetRolesAsync());
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnSuccess_WhenRoleIsCreated()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "ContentEditor",
            Description = "Can edit content but not manage users"
        };

        var roleDto = new RoleDto
        {
            Id = Guid.NewGuid(),
            Name = "ContentEditor",
            NormalizedName = "CONTENTEDITOR",
            Description = "Can edit content but not manage users"
        };

        var apiResponse = ApiResponse<RoleDto>.Ok(roleDto, "Role created successfully");

        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond("application/json", JsonSerializer.Serialize(apiResponse));

        // Act
        var result = await _apiClient.CreateRoleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("ContentEditor");
        result.Data.NormalizedName.Should().Be("CONTENTEDITOR");
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _apiClient.CreateRoleAsync(null!));
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldThrowValidationException_WhenRoleNameIsTaken()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "SuperAdmin",
            Description = "Duplicate role name"
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Role already exists\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CreateRoleAsync(request));
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldThrowValidationException_WhenNameIsInvalid()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "",
            Description = "No name provided"
        };

        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(HttpStatusCode.BadRequest, "application/json",
                "{\"success\":false,\"message\":\"Validation failed\",\"errors\":[\"Name is required\"]}");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _apiClient.CreateRoleAsync(request));
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldHandleRequestCancellation()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "TestRole",
            Description = "Test description"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp
            .When("https://api.test.com/api/v1/admin/roles")
            .Respond(async (req) =>
            {
                await Task.Delay(100);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Act
        var result = await _apiClient.CreateRoleAsync(request, cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cancelled");
    }
}

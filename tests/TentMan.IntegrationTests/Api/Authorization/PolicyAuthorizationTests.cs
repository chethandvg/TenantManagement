using System.Net;
using TentMan.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace TentMan.IntegrationTests.Api.Authorization;

/// <summary>
/// Integration tests for policy-based authorization on API endpoints.
/// Verifies that authorization policies are correctly enforced across different controllers.
/// </summary>
[Collection("Integration Tests")]
public class PolicyAuthorizationTests : IAsyncLifetime
{
    private readonly WebApplicationFactoryFixture _factory;
    private readonly HttpClient _client;
    private readonly Guid _testUserId = Guid.NewGuid();

    public PolicyAuthorizationTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Products Controller Tests

    [Fact]
    public async Task GetProducts_ShouldReturn401_WhenNoToken()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Manager")]
    [InlineData("Administrator")]
    [InlineData("SuperAdmin")]
    public async Task GetProducts_ShouldReturn200_WhenHasRequiredRole(string role)
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync(role, _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_ShouldReturn403_WhenUserHasGuestRole()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Guest", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProducts_ShouldReturn403_WhenUserHasTenantRole()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Tenant", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Audit Logs Controller Tests

    [Fact]
    public async Task GetAuditLogs_ShouldReturn401_WhenNoToken()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Administrator")]
    [InlineData("SuperAdmin")]
    public async Task GetAuditLogs_ShouldReturn200_WhenHasManagerOrHigherRole(string role)
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync(role, _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Guest")]
    [InlineData("Tenant")]
    public async Task GetAuditLogs_ShouldReturn403_WhenUserLacksManagerRole(string role)
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync(role, _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/audit-logs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Tenant Portal Controller Tests

    [Fact]
    public async Task TenantPortal_ShouldReturn401_WhenNoToken()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/v1/tenant-portal/lease-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantPortal_ShouldReturn200OrNotFound_WhenHasTenantRole()
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync("Tenant", _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/tenant-portal/lease-summary");

        // Assert
        // Can be 200 (if lease exists) or 404 (if no lease)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Manager")]
    [InlineData("Administrator")]
    [InlineData("SuperAdmin")]
    [InlineData("Guest")]
    public async Task TenantPortal_ShouldReturn403_WhenUserIsNotTenant(string role)
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync(role, _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/tenant-portal/lease-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Leases Controller Tests

    [Fact]
    public async Task CreateLease_ShouldReturn401_WhenNoToken()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var requestBody = new
        {
            unitId = Guid.NewGuid(),
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddMonths(12)
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/leases", requestBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Administrator")]
    [InlineData("SuperAdmin")]
    public async Task CreateLease_ShouldReturn400OrNotFound_WhenHasManagerOrHigherRole(string role)
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync(role, _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var orgId = Guid.NewGuid();
        var requestBody = new
        {
            unitId = Guid.NewGuid(),
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddMonths(12)
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/leases", requestBody);

        // Assert
        // Can be 400 (validation error) or 404 (org not found), but NOT 403
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Guest")]
    [InlineData("Tenant")]
    public async Task CreateLease_ShouldReturn403_WhenUserLacksManagerRole(string role)
    {
        // Arrange
        var token = await _factory.GetJwtTokenAsync(role, _testUserId.ToString());
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var orgId = Guid.NewGuid();
        var requestBody = new
        {
            unitId = Guid.NewGuid(),
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddMonths(12)
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId}/leases", requestBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Cross-Cutting Authorization Tests

    [Fact]
    public async Task MultipleEndpoints_ShouldConsistentlyEnforceAuthorization()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/v1/products",
            "/api/v1/audit-logs",
            "/api/v1/buildings"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"endpoint {endpoint} should require authentication");
        }
    }

    [Fact]
    public async Task InvalidToken_ShouldReturn401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredToken_ShouldReturn401()
    {
        // Arrange
        var expiredToken = await _factory.GetJwtTokenAsync("User", _testUserId.ToString(), expiresInMinutes: -1);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}

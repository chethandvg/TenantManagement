using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Persistence;
using Archu.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Archu.IntegrationTests.Api.Admin;

[Collection("Integration Tests")]
public class PermissionsEndpointTests
{
    private readonly WebApplicationFactoryFixture _factory;
    private readonly HttpClient _client;
    private ApplicationPermission _alphaPermission = null!;
    private ApplicationPermission _betaPermission = null!;

    public PermissionsEndpointTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Validates that the permission catalog endpoint returns all persisted permissions.
    /// </summary>
    [Fact]
    public async Task GetPermissions_ShouldReturnCatalog()
    {
        await ResetAndSeedAsync();

        var token = await _factory.GetJwtTokenAsync("Manager");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/admin/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<PermissionDto>>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Should().Contain(p => p.Id == _alphaPermission.Id);
        payload.Data.Should().Contain(p => p.Id == _betaPermission.Id);
    }

    private async Task ResetAndSeedAsync()
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _alphaPermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Admin.Roles.View",
            NormalizedName = "ADMIN.ROLES.VIEW",
            Description = "Allows viewing roles"
        };

        _betaPermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Admin.Roles.Create",
            NormalizedName = "ADMIN.ROLES.CREATE",
            Description = "Allows creating roles"
        };

        dbContext.Permissions.AddRange(_alphaPermission, _betaPermission);
        await dbContext.SaveChangesAsync();
    }
}

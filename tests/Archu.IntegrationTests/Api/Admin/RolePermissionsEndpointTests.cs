using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Persistence;
using Archu.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Archu.IntegrationTests.Api.Admin;

[Collection("Integration Tests")]
public class RolePermissionsEndpointTests
{
    private readonly WebApplicationFactoryFixture _factory;
    private readonly HttpClient _client;
    private Guid _roleId;
    private ApplicationPermission _viewPermission = null!;
    private ApplicationPermission _editPermission = null!;

    public RolePermissionsEndpointTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Ensures that the GET endpoint returns all permissions currently linked to the target role.
    /// </summary>
    [Fact]
    public async Task GetRolePermissions_ShouldReturnAssignedPermissions()
    {
        await ResetAndSeedAsync(async dbContext =>
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = _roleId,
                PermissionId = _viewPermission.Id
            });
            await dbContext.SaveChangesAsync();
        });

        var token = await _factory.GetJwtTokenAsync("Administrator");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/admin/roles/{_roleId}/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RolePermissionsDto>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Permissions.Should().ContainSingle(p => p.Id == _viewPermission.Id);
    }

    /// <summary>
    /// Verifies that assigning permissions to a role persists the link and returns the updated projection.
    /// </summary>
    [Fact]
    public async Task AssignPermissions_ShouldAddPermissions()
    {
        await ResetAndSeedAsync();

        var token = await _factory.GetJwtTokenAsync("SuperAdmin");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new ModifyRolePermissionsRequest
        {
            PermissionNames = new[] { _viewPermission.Name }
        };

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/roles/{_roleId}/permissions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RolePermissionsDto>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Permissions.Should().ContainSingle(p => p.NormalizedName == _viewPermission.NormalizedName);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedLinks = await dbContext.RolePermissions.CountAsync(link => link.RoleId == _roleId);
        persistedLinks.Should().Be(1);
    }

    /// <summary>
    /// Confirms that removing role permissions deletes the association and returns the remaining permissions.
    /// </summary>
    [Fact]
    public async Task RemovePermissions_ShouldRemoveAssignments()
    {
        await ResetAndSeedAsync(async dbContext =>
        {
            dbContext.RolePermissions.AddRange(
                new RolePermission { RoleId = _roleId, PermissionId = _viewPermission.Id },
                new RolePermission { RoleId = _roleId, PermissionId = _editPermission.Id });
            await dbContext.SaveChangesAsync();
        });

        var token = await _factory.GetJwtTokenAsync("SuperAdmin");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new ModifyRolePermissionsRequest
        {
            PermissionNames = new[] { _viewPermission.Name }
        };

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/roles/{_roleId}/permissions")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(deleteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<RolePermissionsDto>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Permissions.Should().ContainSingle(p => p.Id == _editPermission.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var remainingLinks = await dbContext.RolePermissions
            .Where(link => link.RoleId == _roleId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();

        remainingLinks.Should().ContainSingle().Which.Should().Be(_editPermission.Id);
    }

    private async Task ResetAndSeedAsync(Func<ApplicationDbContext, Task>? additionalSetup = null)
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = "SupportAgent",
            NormalizedName = "SUPPORTAGENT",
            Description = "Support role for integration testing"
        };

        var viewPermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Admin.Users.View",
            NormalizedName = "ADMIN.USERS.VIEW",
            Description = "Allows viewing users"
        };

        var editPermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Admin.Users.Update",
            NormalizedName = "ADMIN.USERS.UPDATE",
            Description = "Allows editing users"
        };

        dbContext.Roles.Add(role);
        dbContext.Permissions.AddRange(viewPermission, editPermission);

        await dbContext.SaveChangesAsync();

        _roleId = role.Id;
        _viewPermission = viewPermission;
        _editPermission = editPermission;

        if (additionalSetup is not null)
        {
            await additionalSetup(dbContext);
        }
    }
}

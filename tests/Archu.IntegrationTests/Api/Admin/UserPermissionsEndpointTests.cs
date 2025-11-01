using System;
using System.Linq;
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
public class UserPermissionsEndpointTests
{
    private readonly WebApplicationFactoryFixture _factory;
    private readonly HttpClient _client;
    private Guid _userId;
    private Guid _roleId;
    private ApplicationPermission _directPermission = null!;
    private ApplicationPermission _secondaryPermission = null!;
    private ApplicationPermission _rolePermission = null!;

    public UserPermissionsEndpointTests(WebApplicationFactoryFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Ensures that the direct permissions endpoint returns explicit assignments for the user.
    /// </summary>
    [Fact]
    public async Task GetDirectPermissions_ShouldReturnDirectAssignments()
    {
        await ResetAndSeedAsync(async dbContext =>
        {
            dbContext.UserPermissions.Add(new UserPermission
            {
                UserId = _userId,
                PermissionId = _directPermission.Id
            });
            await dbContext.SaveChangesAsync();
        });

        var token = await _factory.GetJwtTokenAsync("Administrator");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/admin/users/{_userId}/permissions/direct");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserPermissionsDto>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Permissions.Should().ContainSingle(p => p.Id == _directPermission.Id);
    }

    /// <summary>
    /// Verifies that the effective permissions endpoint aggregates role and direct assignments.
    /// </summary>
    [Fact]
    public async Task GetEffectivePermissions_ShouldReturnCombinedPermissions()
    {
        await ResetAndSeedAsync(async dbContext =>
        {
            dbContext.UserPermissions.Add(new UserPermission
            {
                UserId = _userId,
                PermissionId = _directPermission.Id
            });

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = _roleId,
                PermissionId = _rolePermission.Id
            });

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = _userId,
                RoleId = _roleId,
                AssignedAtUtc = DateTime.UtcNow,
                AssignedBy = "integration-test"
            });

            await dbContext.SaveChangesAsync();
        });

        var token = await _factory.GetJwtTokenAsync("Manager");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/admin/users/{_userId}/permissions/effective");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<EffectiveUserPermissionsDto>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.DirectPermissions.Should().ContainSingle(p => p.Id == _directPermission.Id);
        payload.Data.RolePermissions.Should().Contain(rp => rp.Permissions.Any(p => p.Id == _rolePermission.Id));
        payload.Data.EffectivePermissions.Should().HaveCount(2);
    }

    /// <summary>
    /// Confirms that assigning permissions via the API creates the direct linkage.
    /// </summary>
    [Fact]
    public async Task AssignPermissions_ShouldPersistDirectAssignments()
    {
        await ResetAndSeedAsync();

        var token = await _factory.GetJwtTokenAsync("SuperAdmin");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new ModifyUserPermissionsRequest
        {
            PermissionNames = new[] { _directPermission.Name }
        };

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/users/{_userId}/permissions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserPermissionsDto>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Permissions.Should().ContainSingle(p => p.Id == _directPermission.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var assignments = await dbContext.UserPermissions.CountAsync(up => up.UserId == _userId);
        assignments.Should().Be(1);
    }

    /// <summary>
    /// Confirms that the remove endpoint deletes existing direct permission links.
    /// </summary>
    [Fact]
    public async Task RemovePermissions_ShouldDeleteDirectAssignments()
    {
        await ResetAndSeedAsync(async dbContext =>
        {
            dbContext.UserPermissions.AddRange(
                new UserPermission { UserId = _userId, PermissionId = _directPermission.Id },
                new UserPermission { UserId = _userId, PermissionId = _secondaryPermission.Id });
            await dbContext.SaveChangesAsync();
        });

        var token = await _factory.GetJwtTokenAsync("SuperAdmin");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new ModifyUserPermissionsRequest
        {
            PermissionNames = new[] { _directPermission.Name }
        };

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/users/{_userId}/permissions")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(deleteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<UserPermissionsDto>>();
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Permissions.Should().ContainSingle(p => p.Id == _secondaryPermission.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var remainingAssignments = await dbContext.UserPermissions
            .Where(up => up.UserId == _userId)
            .Select(up => up.PermissionId)
            .ToArrayAsync();

        remainingAssignments.Should().ContainSingle().Which.Should().Be(_secondaryPermission.Id);
    }

    private async Task ResetAndSeedAsync(Func<ApplicationDbContext, Task>? additionalSetup = null)
    {
        await _factory.ResetDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "perms-user",
            Email = "perms@example.com",
            NormalizedEmail = "PERMS@EXAMPLE.COM",
            PasswordHash = "test-hash",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = "Analyst",
            NormalizedName = "ANALYST",
            Description = "Analyst role for integration testing"
        };

        _directPermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Admin.Dashboard.View",
            NormalizedName = "ADMIN.DASHBOARD.VIEW",
            Description = "Allows viewing dashboards"
        };

        _secondaryPermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Admin.Dashboard.Edit",
            NormalizedName = "ADMIN.DASHBOARD.EDIT",
            Description = "Allows editing dashboards"
        };

        _rolePermission = new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Admin.Reports.View",
            NormalizedName = "ADMIN.REPORTS.VIEW",
            Description = "Allows viewing reports"
        };

        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.AddRange(_directPermission, _secondaryPermission, _rolePermission);

        await dbContext.SaveChangesAsync();

        _userId = user.Id;
        _roleId = role.Id;

        if (additionalSetup is not null)
        {
            await additionalSetup(dbContext);
        }
    }
}

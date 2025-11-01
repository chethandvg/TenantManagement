using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Archu.IntegrationTests.Infrastructure.Repositories;

public class RolePermissionRepositoryTests
{
    [Fact]
    public async Task LinkPermissionsAsync_AddsOnlyMissingAssignments()
    {
        await using var context = RepositoryTestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var repository = new RolePermissionRepository(context);

        var roleId = Guid.NewGuid();
        var existingPermissionId = Guid.NewGuid();
        var newPermissionId = Guid.NewGuid();

        context.Roles.Add(new ApplicationRole
        {
            Id = roleId,
            Name = "Manager",
            NormalizedName = "MANAGER"
        });

        context.Permissions.AddRange(
            new ApplicationPermission
            {
                Id = existingPermissionId,
                Name = "Products.Read",
                NormalizedName = "PRODUCTS.READ"
            },
            new ApplicationPermission
            {
                Id = newPermissionId,
                Name = "Products.Write",
                NormalizedName = "PRODUCTS.WRITE"
            });

        context.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = existingPermissionId
        });

        await context.SaveChangesAsync();

        await repository.LinkPermissionsAsync(
            roleId,
            new[] { existingPermissionId, newPermissionId, newPermissionId },
            CancellationToken.None);

        await context.SaveChangesAsync();

        var assignments = await context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .ToListAsync();

        assignments.Should().HaveCount(2);
        assignments.Select(rolePermission => rolePermission.PermissionId)
            .Should().Contain(newPermissionId);
    }

    [Fact]
    public async Task UnlinkPermissionsAsync_RemovesExistingAssignments()
    {
        await using var context = RepositoryTestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var repository = new RolePermissionRepository(context);

        var roleId = Guid.NewGuid();
        var retainedPermissionId = Guid.NewGuid();
        var removedPermissionId = Guid.NewGuid();

        context.Roles.Add(new ApplicationRole
        {
            Id = roleId,
            Name = "Support",
            NormalizedName = "SUPPORT"
        });

        context.Permissions.AddRange(
            new ApplicationPermission
            {
                Id = retainedPermissionId,
                Name = "Tickets.Read",
                NormalizedName = "TICKETS.READ"
            },
            new ApplicationPermission
            {
                Id = removedPermissionId,
                Name = "Tickets.Close",
                NormalizedName = "TICKETS.CLOSE"
            });

        context.RolePermissions.AddRange(
            new RolePermission { RoleId = roleId, PermissionId = retainedPermissionId },
            new RolePermission { RoleId = roleId, PermissionId = removedPermissionId });

        await context.SaveChangesAsync();

        await repository.UnlinkPermissionsAsync(
            roleId,
            new[] { removedPermissionId, Guid.NewGuid() },
            CancellationToken.None);

        await context.SaveChangesAsync();

        var assignments = await context.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .ToListAsync();

        assignments.Should().HaveCount(1);
        assignments.Single().PermissionId.Should().Be(retainedPermissionId);
    }

    [Fact]
    public async Task GetPermissionNamesByRoleIdsAsync_ReturnsDistinctNormalizedNames()
    {
        await using var context = RepositoryTestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var repository = new RolePermissionRepository(context);

        var roleId = Guid.NewGuid();

        context.Roles.Add(new ApplicationRole
        {
            Id = roleId,
            Name = "Auditor",
            NormalizedName = "AUDITOR"
        });

        var permissionIds = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        context.Permissions.AddRange(
            new ApplicationPermission
            {
                Id = permissionIds[0],
                Name = "Reports.Read",
                NormalizedName = "REPORTS.READ"
            },
            new ApplicationPermission
            {
                Id = permissionIds[1],
                Name = "Reports.Export",
                NormalizedName = "REPORTS.EXPORT"
            });

        context.RolePermissions.AddRange(
            new RolePermission { RoleId = roleId, PermissionId = permissionIds[0] },
            new RolePermission { RoleId = roleId, PermissionId = permissionIds[1] },
            new RolePermission { RoleId = roleId, PermissionId = permissionIds[0] });

        await context.SaveChangesAsync();

        var names = await repository.GetPermissionNamesByRoleIdsAsync(new[] { roleId }, CancellationToken.None);

        names.Should().BeEquivalentTo(new[] { "REPORTS.READ", "REPORTS.EXPORT" });
    }
}

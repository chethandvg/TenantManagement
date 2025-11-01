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

public class UserPermissionRepositoryTests
{
    [Fact]
    public async Task LinkPermissionsAsync_AddsOnlyMissingAssignments()
    {
        await using var context = RepositoryTestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var repository = new UserPermissionRepository(context);

        var userId = Guid.NewGuid();
        var existingPermissionId = Guid.NewGuid();
        var newPermissionId = Guid.NewGuid();

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "alice",
            Email = "alice@example.com",
            NormalizedEmail = "ALICE@EXAMPLE.COM",
            PasswordHash = "hash",
            SecurityStamp = Guid.NewGuid().ToString()
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

        context.UserPermissions.Add(new UserPermission
        {
            UserId = userId,
            PermissionId = existingPermissionId
        });

        await context.SaveChangesAsync();

        await repository.LinkPermissionsAsync(
            userId,
            new[] { existingPermissionId, newPermissionId, newPermissionId },
            CancellationToken.None);

        await context.SaveChangesAsync();

        var assignments = await context.UserPermissions
            .Where(userPermission => userPermission.UserId == userId)
            .ToListAsync();

        assignments.Should().HaveCount(2);
        assignments.Select(userPermission => userPermission.PermissionId)
            .Should().Contain(newPermissionId);
    }

    [Fact]
    public async Task UnlinkPermissionsAsync_RemovesExistingAssignments()
    {
        await using var context = RepositoryTestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var repository = new UserPermissionRepository(context);

        var userId = Guid.NewGuid();
        var retainedPermissionId = Guid.NewGuid();
        var removedPermissionId = Guid.NewGuid();

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "bob",
            Email = "bob@example.com",
            NormalizedEmail = "BOB@EXAMPLE.COM",
            PasswordHash = "hash",
            SecurityStamp = Guid.NewGuid().ToString()
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

        context.UserPermissions.AddRange(
            new UserPermission { UserId = userId, PermissionId = retainedPermissionId },
            new UserPermission { UserId = userId, PermissionId = removedPermissionId });

        await context.SaveChangesAsync();

        await repository.UnlinkPermissionsAsync(
            userId,
            new[] { removedPermissionId, Guid.NewGuid() },
            CancellationToken.None);

        await context.SaveChangesAsync();

        var assignments = await context.UserPermissions
            .Where(userPermission => userPermission.UserId == userId)
            .ToListAsync();

        assignments.Should().HaveCount(1);
        assignments.Single().PermissionId.Should().Be(retainedPermissionId);
    }

    [Fact]
    public async Task GetPermissionNamesByUserIdAsync_ReturnsDistinctNormalizedNames()
    {
        await using var context = RepositoryTestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var repository = new UserPermissionRepository(context);

        var userId = Guid.NewGuid();

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "carol",
            Email = "carol@example.com",
            NormalizedEmail = "CAROL@EXAMPLE.COM",
            PasswordHash = "hash",
            SecurityStamp = Guid.NewGuid().ToString()
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

        context.UserPermissions.AddRange(
            new UserPermission { UserId = userId, PermissionId = permissionIds[0] },
            new UserPermission { UserId = userId, PermissionId = permissionIds[1] },
            new UserPermission { UserId = userId, PermissionId = permissionIds[0] });

        await context.SaveChangesAsync();

        var names = await repository.GetPermissionNamesByUserIdAsync(userId, CancellationToken.None);

        names.Should().BeEquivalentTo(new[] { "REPORTS.READ", "REPORTS.EXPORT" });
    }

    [Fact]
    public async Task GetByUserIdsAsync_ReturnsMatchingAssignments()
    {
        await using var context = RepositoryTestContextFactory.CreateContext(Guid.NewGuid().ToString());
        var repository = new UserPermissionRepository(context);

        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        context.Users.AddRange(
            new ApplicationUser
            {
                Id = firstUserId,
                UserName = "dave",
                Email = "dave@example.com",
                NormalizedEmail = "DAVE@EXAMPLE.COM",
                PasswordHash = "hash",
                SecurityStamp = Guid.NewGuid().ToString()
            },
            new ApplicationUser
            {
                Id = secondUserId,
                UserName = "erin",
                Email = "erin@example.com",
                NormalizedEmail = "ERIN@EXAMPLE.COM",
                PasswordHash = "hash",
                SecurityStamp = Guid.NewGuid().ToString()
            });

        context.Permissions.Add(new ApplicationPermission
        {
            Id = permissionId,
            Name = "Inventory.Read",
            NormalizedName = "INVENTORY.READ"
        });

        context.UserPermissions.AddRange(
            new UserPermission { UserId = firstUserId, PermissionId = permissionId },
            new UserPermission { UserId = secondUserId, PermissionId = permissionId });

        await context.SaveChangesAsync();

        var assignments = await repository.GetByUserIdsAsync(new[] { firstUserId }, CancellationToken.None);

        assignments.Should().ContainSingle(assignment => assignment.UserId == firstUserId);
    }
}

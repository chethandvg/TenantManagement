using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archu.Domain.Entities.Identity;
using Archu.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace Archu.IntegrationTests.Infrastructure.Repositories;

[Collection("Repository Tests Docker")]
public class PermissionRepositoryTests : IAsyncLifetime
{
    private readonly DockerRepositoryTestContextFactory _factory;

    public PermissionRepositoryTests(DockerRepositoryTestContextFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.CleanDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetByNormalizedNamesAsync_ReturnsMatchingPermissions()
    {
        await using var context = _factory.CreateContext();
        var repository = new PermissionRepository(context);

        var permissionId = Guid.NewGuid();
        context.Permissions.AddRange(
            new ApplicationPermission
            {
                Id = permissionId,
                Name = "Products.Read",
                NormalizedName = "PRODUCTS.READ"
            },
            new ApplicationPermission
            {
                Id = Guid.NewGuid(),
                Name = "Products.Write",
                NormalizedName = "PRODUCTS.WRITE"
            });

        await context.SaveChangesAsync();

        var results = await repository.GetByNormalizedNamesAsync(
            new[] { "products.read", "unknown", string.Empty },
            CancellationToken.None);

        results.Should().ContainSingle(permission => permission.Id == permissionId);
    }

    [Fact]
    public async Task GetByNormalizedNamesAsync_IgnoresEmptyRequests()
    {
        await using var context = _factory.CreateContext();
        var repository = new PermissionRepository(context);

        context.Permissions.Add(new ApplicationPermission
        {
            Id = Guid.NewGuid(),
            Name = "Products.Delete",
            NormalizedName = "PRODUCTS.DELETE"
        });

        await context.SaveChangesAsync();

        var results = await repository.GetByNormalizedNamesAsync(Array.Empty<string>(), CancellationToken.None);

        results.Should().BeEmpty();
    }
}

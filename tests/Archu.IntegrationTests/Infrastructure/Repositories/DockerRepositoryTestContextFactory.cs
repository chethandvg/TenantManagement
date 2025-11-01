using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archu.Application.Abstractions;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Archu.IntegrationTests.Infrastructure.Repositories;

/// <summary>
/// Factory for creating ApplicationDbContext instances backed by a SQL Server Docker container for repository tests.
/// </summary>
public class DockerRepositoryTestContextFactory : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;
    private string _connectionString = string.Empty;

    public DockerRepositoryTestContextFactory()
    {
        _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _connectionString = _dbContainer.GetConnectionString();

      // Create the database schema using EnsureCreated instead of migrations
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
 await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates a new ApplicationDbContext connected to the Docker SQL Server container.
    /// </summary>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
.UseSqlServer(_connectionString)
  .ConfigureWarnings(warnings => 
      warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
         .Options;

     return new ApplicationDbContext(
            options,
   new TestCurrentUser(),
    new TestTimeProvider());
    }

    /// <summary>
    /// Cleans all data from the database while preserving schema.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateContext();

        // Delete data from all tables in reverse order of dependencies
        await context.Database.ExecuteSqlRawAsync(@"
  DELETE FROM [RolePermissions];
          DELETE FROM [UserPermissions];
     DELETE FROM [UserRoles];
            DELETE FROM [Permissions];
        DELETE FROM [Roles];
   DELETE FROM [Products];
    DELETE FROM [Users];
        ");
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId { get; } = Guid.Empty.ToString();

      public bool IsAuthenticated => true;

        public bool IsInRole(string role) => false;

        public bool HasAnyRole(params string[] roles) => false;

    public IEnumerable<string> GetRoles() => Array.Empty<string>();
    }

    private sealed class TestTimeProvider : ITimeProvider
    {
      public DateTime UtcNow { get; } = DateTime.UtcNow;
    }
}

/// <summary>
/// Collection definition for repository tests using Docker SQL Server container.
/// All tests in this collection share the same container instance.
/// </summary>
[CollectionDefinition("Repository Tests Docker")]
public class RepositoryTestCollection : ICollectionFixture<DockerRepositoryTestContextFactory>
{
}

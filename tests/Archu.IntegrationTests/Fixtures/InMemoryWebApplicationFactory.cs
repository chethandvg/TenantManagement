using Archu.Application.Abstractions;
using Archu.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Archu.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory using in-memory database for integration testing.
/// No real database required - fast, isolated tests with mock data.
/// </summary>
public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    /// <summary>
    /// Initialize the test application (called before each test collection).
    /// </summary>
    public Task InitializeAsync()
    {
        // Create and migrate the in-memory database
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleanup after tests (called after each test collection).
    /// </summary>
    public new async Task DisposeAsync()
    {
        // Clean up in-memory database
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureDeleted();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing ApplicationDbContext registration
            var dbContextDescriptors = services.Where(d => d.ServiceType == typeof(ApplicationDbContext) || d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList();
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register ApplicationDbContext with in-memory database
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName);
                
                // Disable change tracking for tests (optional, but faster)
                options.ConfigureWarnings(w =>
                {
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
                });
            });

            // Register test implementations of ICurrentUser and ITimeProvider
            var currentUserDescriptors = services.Where(d => d.ServiceType == typeof(ICurrentUser)).ToList();
            var timeProviderDescriptors = services.Where(d => d.ServiceType == typeof(ITimeProvider)).ToList();
            
            foreach (var descriptor in currentUserDescriptors.Concat(timeProviderDescriptors))
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<ICurrentUser, TestCurrentUser>();
            services.AddSingleton<ITimeProvider, TestTimeProvider>();
        });
    }

    /// <summary>
    /// Reset the in-memory database to clean state.
    /// Call this in test setup to ensure isolation between tests.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Clear all data
        db.Products.RemoveRange(db.Products);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Get the ApplicationDbContext for seeding test data.
    /// </summary>
    public ApplicationDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Test implementation of ICurrentUser for integration tests.
    /// Returns a default authenticated user for testing.
    /// </summary>
    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId => "00000000-0000-0000-0000-000000000001"; // Well-known test user ID
        public bool IsAuthenticated => true;

        public bool IsInRole(string role)
        {
            return role == "User" || role == "Manager" || role == "Administrator";
        }

        public bool HasAnyRole(params string[] roles)
        {
            return roles.Contains("User") || roles.Contains("Manager") || roles.Contains("Administrator");
        }

        public IEnumerable<string> GetRoles()
        {
            yield return "User";
        }
    }

    /// <summary>
    /// Test implementation of ITimeProvider for integration tests.
    /// Returns current UTC time.
    /// </summary>
    private sealed class TestTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

/// <summary>
/// Collection definition for integration tests using in-memory database.
/// All tests in this collection share the same WebApplicationFactory instance.
/// </summary>
[CollectionDefinition("Integration Tests InMemory")]
public class InMemoryIntegrationTestCollection : ICollectionFixture<InMemoryWebApplicationFactory>
{
}

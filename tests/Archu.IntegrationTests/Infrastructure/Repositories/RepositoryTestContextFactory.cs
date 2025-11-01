using System;
using System.Collections.Generic;
using Archu.Application.Abstractions;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Archu.IntegrationTests.Infrastructure.Repositories;

internal static class RepositoryTestContextFactory
{
    public static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(
            options,
            new TestCurrentUser(),
            new TestTimeProvider());
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

using Archu.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Archu.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Point this at your local/Dev connection string for migrations
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = cfg.GetConnectionString("Sql")
                 ?? "Server=localhost;Database=AcmeDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(cs, sql =>
            {
                sql.EnableRetryOnFailure();
                sql.CommandTimeout(30);
            })
            .Options;

        // Minimal dummy services for design-time (tools don’t use them)
        var currentUser = new DesignTimeCurrentUser();
        var time = new SystemTimeProvider();

        return new ApplicationDbContext(opts, currentUser, time);
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public string? UserId => "design-time";
    }

    private sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.SqlServer;

var builder = DistributedApplication.CreateBuilder(args);

var sql = ConfigureSqlResource(builder);
ConfigureApiResource(builder, sql);

builder.Build().Run();

/// <summary>
/// Configures the dev-time SQL Server resource that backs the application data store.
/// </summary>
static IResourceBuilder<SqlServerResource> ConfigureSqlResource(DistributedApplicationBuilder builder)
{
    return builder
        .AddSqlServer("sql")
        .WithDataVolume();
}

/// <summary>
/// Registers the API project, links its dependencies, and exposes HTTP endpoints for the Scalar UI.
/// </summary>
static IResourceBuilder<ProjectResource> ConfigureApiResource(
    DistributedApplicationBuilder builder,
    IResourceBuilder<SqlServerResource> sql)
{
    return builder
        .AddProject<Projects.Archu_Api>("api")
        .WithReference(sql)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithExternalHttpEndpoints();
}

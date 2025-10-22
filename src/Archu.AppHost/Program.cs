using Archu.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// DATABASE CONFIGURATION TOGGLE
// ============================================
// Uncomment ONE of the following lines to choose your database:

const bool useDockerDatabase = true;  // Set to false to use local SQL Server

// ============================================

if (useDockerDatabase)
{
    // Using Docker SQL Server with persistent volume
    var sql = builder.AddSqlServer("sql").WithDataVolume()
        .AddDatabase("archudb");

    // Main API
    var api = builder.AddProject<Projects.Archu_Api>("api")
        .WithReference(sql)  // This injects the Docker connection string
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithExternalHttpEndpoints()
        .WithScalar();

    // Admin API - shares the same database
    var adminApi = builder.AddProject<Projects.Archu_AdminApi>("admin-api")
        .WithReference(sql)  // Share the same database with main API
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithExternalHttpEndpoints()
        .WithScalar();
}
else
{
    // Using local SQL Server (connection string from appsettings.Development.json)
    
    // Main API
    var api = builder.AddProject<Projects.Archu_Api>("api")
        // No .WithReference(sql) - API will use connection string from appsettings.Development.json
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithExternalHttpEndpoints()
        .WithScalar();

    // Admin API
    var adminApi = builder.AddProject<Projects.Archu_AdminApi>("admin-api")
        // No .WithReference(sql) - Admin API will use connection string from appsettings.Development.json
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithExternalHttpEndpoints()
        .WithScalar();
}

builder.Build().Run();

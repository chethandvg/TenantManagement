using Archu.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// DATABASE CONFIGURATION TOGGLE
// ============================================
// Configure via environment variable or default to Docker
// Set ARCHU_USE_LOCAL_DB=true to use local SQL Server
var useLocalDatabase = builder.Configuration["ARCHU_USE_LOCAL_DB"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
var useDockerDatabase = !useLocalDatabase;

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

    // Blazor WebAssembly - references the API
    var web = builder.AddProject<Projects.Archu_Web>("web")
        .WithReference(api)  // This configures the API URL for the Web app
        .WithExternalHttpEndpoints();
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

    // Blazor WebAssembly - references the API
    var web = builder.AddProject<Projects.Archu_Web>("web")
        .WithReference(api)  // This configures the API URL for the Web app
        .WithExternalHttpEndpoints();
}

builder.Build().Run();

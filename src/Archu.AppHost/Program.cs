var builder = DistributedApplication.CreateBuilder(args);

// Configure dev-time SQL Server container to back the API locally and persist data between runs.
var sql = builder.AddSqlServer("sql")
                 .WithDataVolume();

// Register the API, connect it to SQL, and expose HTTP endpoints externally for Scalar UI access.
builder.AddProject<Projects.Archu_Api>("api")
       .WithReference(sql)
       .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
       .WithExternalHttpEndpoints();

builder.Build().Run();

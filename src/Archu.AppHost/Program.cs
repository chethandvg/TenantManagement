var builder = DistributedApplication.CreateBuilder(args);

// Dev-time SQL Server (container) for local runs
var sql = builder.AddSqlServer("sql")
                 .WithDataVolume(); // keep data between runs (optional)

// Wire connection string into API
var api = builder.AddProject<Projects.Archu_Api>("api")
                 .WithReference(sql);

builder.Build().Run();

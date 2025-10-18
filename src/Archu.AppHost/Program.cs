using Archu.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithReference(sql)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints()
    .WithScalar();

builder.Build().Run();

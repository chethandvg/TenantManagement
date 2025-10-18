using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

var builder = DistributedApplication.CreateBuilder(args);

// Configure dev-time SQL Server container to back the API locally and persist data between runs.
var sql = builder.AddSqlServer("sql")
                 .WithDataVolume();

// Register the API, connect it to SQL, and expose HTTP endpoints externally for Scalar UI access.
var apiService = builder.AddProject<Projects.Archu_Api>("api")
                        .WithReference(sql)
                        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                        .WithExternalHttpEndpoints();

// Register a dashboard command that opens the Scalar API documentation for quick access during development workflows.
apiService.WithCommand("scalar-ui-docs", "Scalar API Documentation", async context =>
{
    // Launch the Scalar API documentation UI in the user's default browser when the Aspire dashboard command is executed.
    var endpoint = apiService.GetEndpoint("https") ?? apiService.GetEndpoint("http");

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        return CommandResults.Failure("The API endpoint is unavailable. Ensure the service is running before launching Scalar UI.");
    }

    var scalarUrl = $"{endpoint.TrimEnd('/')}/scalar/v1";

    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = scalarUrl,
            UseShellExecute = true
        });

        return CommandResults.Success();
    }
    catch (Exception ex)
    {
        return CommandResults.Failure($"Unable to launch Scalar UI: {ex.Message}");
    }
});

builder.Build().Run();

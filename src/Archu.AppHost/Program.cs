using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithReference(sql)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints();

api.WithCommand(
    name: "scalar-ui-docs",
    displayName: "Scalar API Documentation",
    executeCommand: async context =>
    {
        // Prefer https; fall back to http
        var endpoint = api.GetEndpoint("https") ?? api.GetEndpoint("http");
        if (endpoint is null)
            return CommandResults.Failure("The API endpoint is unavailable. Ensure the service is running.");

        // Resolve the concrete URL at runtime
        var endpointUrl = await endpoint
            .Property(EndpointProperty.Url)
            .GetValueAsync(context.CancellationToken);

        if (string.IsNullOrWhiteSpace(endpointUrl))
            return CommandResults.Failure("Could not resolve the API endpoint URL.");

        var scalarUrl = $"{endpointUrl.TrimEnd('/')}/scalar/v1";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = scalarUrl, UseShellExecute = true });
            return CommandResults.Success();
        }
        catch (Exception ex)
        {
            return CommandResults.Failure($"Unable to launch Scalar UI: {ex.Message}");
        }
    },
    commandOptions: new CommandOptions   // <- disambiguates to the new overload
    {
        Description = "Open API docs in Scalar UI",
        IconName = "BookQuestionMark",
        IconVariant = IconVariant.Filled
    });

builder.Build().Run();

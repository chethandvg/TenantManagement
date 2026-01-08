using System.Diagnostics;

namespace TentMan.AppHost;

internal static class ResourceBuilderExtensions
{
    internal static IResourceBuilder<T> WithScalar<T>(this IResourceBuilder<T> builder) where T : IResourceWithEndpoints
    {
        return builder.WithOpenApiDocs(
            name: "scalar-ui-docs",
            displayName: "Scalar API Documentation",
            openApiUiPath: "/scalar/v1");
    }
    private static IResourceBuilder<T> WithOpenApiDocs<T>(this IResourceBuilder<T> builder, string name, string displayName, string openApiUiPath) where T : IResourceWithEndpoints
    {
        return builder.WithCommand(
    name: name,
    displayName: displayName,
    executeCommand: async context =>
    {
        // Prefer https; fall back to http
        var endpoint = builder.GetEndpoint("https") ?? builder.GetEndpoint("http");
        if (endpoint is null)
            return CommandResults.Failure("The API endpoint is unavailable. Ensure the service is running.");

        // Resolve the concrete URL at runtime
        var endpointUrl = await endpoint
            .Property(EndpointProperty.Url)
            .GetValueAsync(context.CancellationToken);

        if (string.IsNullOrWhiteSpace(endpointUrl))
            return CommandResults.Failure("Could not resolve the API endpoint URL.");

        var scalarUrl = $"{endpointUrl.TrimEnd('/')}{openApiUiPath}";

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
    }
}

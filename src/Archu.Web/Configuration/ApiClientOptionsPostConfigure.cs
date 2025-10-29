using Archu.ApiClient.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Archu.Web.Configuration;

/// <summary>
/// Applies post-configuration logic for <see cref="ApiClientOptions"/> that depends on dynamic environment data.
/// </summary>
public sealed class ApiClientOptionsPostConfigure : IPostConfigureOptions<ApiClientOptions>
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientOptionsPostConfigure"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration used to resolve service discovery overrides.</param>
    public ApiClientOptionsPostConfigure(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Ensures the API client base URL falls back to Aspire service discovery values when not provided in configuration.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    public void PostConfigure(string? name, ApiClientOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return;
        }

        options.BaseUrl =
            _configuration["services:api:https:0"] ??
            _configuration["services:api:http:0"] ??
            "https://localhost:7123";
    }
}

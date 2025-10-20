using Archu.ApiClient.Configuration;
using Archu.ApiClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Archu.ApiClient.Extensions;

/// <summary>
/// Extension methods for configuring API client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the API client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<ApiClientOptions>(
            configuration.GetSection(ApiClientOptions.SectionName));

        var options = configuration
            .GetSection(ApiClientOptions.SectionName)
            .Get<ApiClientOptions>() ?? new ApiClientOptions();

        // Add HttpClient with Polly retry policy
        services.AddHttpClient<IProductsApiClient, ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler((serviceProvider, request) => 
            GetRetryPolicy(options.RetryCount, serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()))
        .AddPolicyHandler((serviceProvider, request) => 
            GetCircuitBreakerPolicy(serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()));

        return services;
    }

    /// <summary>
    /// Adds the API client services to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the API client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClient(
        this IServiceCollection services,
        Action<ApiClientOptions> configureOptions)
    {
        services.Configure(configureOptions);

        var options = new ApiClientOptions();
        configureOptions(options);

        services.AddHttpClient<IProductsApiClient, ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler((serviceProvider, request) => 
            GetRetryPolicy(options.RetryCount, serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()))
        .AddPolicyHandler((serviceProvider, request) => 
            GetCircuitBreakerPolicy(serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()));

        return services;
    }

    /// <summary>
    /// Creates a retry policy with exponential backoff.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount, ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        "Request failed with {StatusCode}. Waiting {Delay}s before retry attempt {RetryAttempt}/{RetryCount}. Request: {RequestUri}",
                        outcome.Result?.StatusCode,
                        timespan.TotalSeconds,
                        retryAttempt,
                        retryCount,
                        outcome.Result?.RequestMessage?.RequestUri);
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    logger.LogError(
                        "Circuit breaker opened for {BreakDelay}s due to {StatusCode}. Request: {RequestUri}",
                        breakDelay.TotalSeconds,
                        outcome.Result?.StatusCode,
                        outcome.Result?.RequestMessage?.RequestUri);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset, requests will be allowed through");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker is half-open, testing with next request");
                });
    }
}

using TentMan.AdminApiClient.Configuration;
using TentMan.AdminApiClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace TentMan.AdminApiClient.Extensions;

/// <summary>
/// Extension methods for configuring Admin API client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Admin API client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers all Admin API client services with default configuration from appsettings.json.
    /// Configure the client using the "AdminApiClient" section in appsettings.json.
    /// </remarks>
    public static IServiceCollection AddAdminApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<AdminApiClientOptions>(
            configuration.GetSection(AdminApiClientOptions.SectionName));

        var options = configuration
            .GetSection(AdminApiClientOptions.SectionName)
            .Get<AdminApiClientOptions>() ?? new AdminApiClientOptions();

        ConfigureHttpClients(services, options);

        return services;
    }

    /// <summary>
    /// Adds the Admin API client services to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the Admin API client options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers all Admin API client services with custom configuration.
    /// Use this method when you need to configure options programmatically.
    /// </remarks>
    public static IServiceCollection AddAdminApiClient(
        this IServiceCollection services,
        Action<AdminApiClientOptions> configureOptions)
    {
        services.Configure(configureOptions);

        var options = new AdminApiClientOptions();
        configureOptions(options);

        ConfigureHttpClients(services, options);

        return services;
    }

    /// <summary>
    /// Configures the HttpClients with policies.
    /// </summary>
    private static void ConfigureHttpClients(
        IServiceCollection services,
        AdminApiClientOptions options)
    {
        // Configure Initialization API Client (anonymous access for initial setup)
        ConfigureHttpClient<IInitializationApiClient, InitializationApiClient>(services, options);

        // Configure Roles API Client (requires authentication)
        ConfigureHttpClient<IRolesApiClient, RolesApiClient>(services, options);

        // Configure Users API Client (requires authentication)
        ConfigureHttpClient<IUsersApiClient, UsersApiClient>(services, options);

        // Configure UserRoles API Client (requires authentication)
        ConfigureHttpClient<IUserRolesApiClient, UserRolesApiClient>(services, options);
    }

    /// <summary>
    /// Configures an individual HTTP client.
    /// </summary>
    private static void ConfigureHttpClient<TInterface, TImplementation>(
        IServiceCollection services,
        AdminApiClientOptions options)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var httpClientBuilder = services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Add retry policy if enabled
        if (options.EnableRetryPolicy)
        {
            httpClientBuilder.AddPolicyHandler((serviceProvider, request) =>
                GetRetryPolicy(
                    options.RetryCount,
                    options.RetryBaseDelaySeconds,
                    serviceProvider.GetRequiredService<ILogger<TImplementation>>()));
        }

        // Add circuit breaker if enabled
        if (options.EnableCircuitBreaker)
        {
            httpClientBuilder.AddPolicyHandler((serviceProvider, request) =>
                GetCircuitBreakerPolicy(
                    options.CircuitBreakerFailureThreshold,
                    options.CircuitBreakerDurationSeconds,
                    serviceProvider.GetRequiredService<ILogger<TImplementation>>()));
        }
    }

    /// <summary>
    /// Gets the retry policy with exponential backoff.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        int retryCount,
        double baseDelaySeconds,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * baseDelaySeconds),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode;
                    logger.LogWarning(
                        "Request failed with {StatusCode}. Waiting {Delay}s before retry attempt {RetryAttempt}/{RetryCount}",
                        statusCode.HasValue ? (int)statusCode : 0,
                        timespan.TotalSeconds,
                        retryAttempt,
                        retryCount);
                });
    }

    /// <summary>
    /// Gets the circuit breaker policy.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        int failureThreshold,
        int durationSeconds,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: failureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(durationSeconds),
                onBreak: (outcome, breakDelay) =>
                {
                    var statusCode = outcome.Result?.StatusCode;
                    logger.LogError(
                        "Circuit breaker opened for {BreakDelay}s due to {StatusCode}",
                        breakDelay.TotalSeconds,
                        statusCode.HasValue ? (int)statusCode : 0);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset, requests will be allowed through");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker is half-open, testing if service has recovered");
                });
    }
}

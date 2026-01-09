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
    /// Adds the Admin API client services to the service collection using configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration containing Admin API client settings.</param>
    /// <returns>The service collection for chaining.</returns>
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
        // Configure Users API Client
        ConfigureHttpClient<IUsersApiClient, UsersApiClient>(services, options);

        // Configure Roles API Client
        ConfigureHttpClient<IRolesApiClient, RolesApiClient>(services, options);

        // Configure UserRoles API Client
        ConfigureHttpClient<IUserRolesApiClient, UserRolesApiClient>(services, options);

        // Configure Initialization API Client
        ConfigureHttpClient<IInitializationApiClient, InitializationApiClient>(services, options);
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
    /// Creates a retry policy with exponential backoff.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        int retryCount,
        double baseDelaySeconds,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>() // Handle timeouts
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(baseDelaySeconds * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "N/A";
                    var exceptionType = outcome.Exception?.GetType().Name ?? "None";

                    logger.LogWarning(
                        "Request to {RequestUri} failed (Status: {StatusCode}, Exception: {ExceptionType}). " +
                        "Waiting {Delay:0.00}s before retry {RetryAttempt}/{RetryCount}",
                        outcome.Result?.RequestMessage?.RequestUri ?? context.GetValueOrDefault("RequestUri"),
                        statusCode,
                        exceptionType,
                        timespan.TotalSeconds,
                        retryAttempt,
                        retryCount);
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        int failureThreshold,
        int durationSeconds,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>() // Handle timeouts
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: failureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(durationSeconds),
                onBreak: (outcome, breakDelay) =>
                {
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "N/A";
                    var exceptionType = outcome.Exception?.GetType().Name ?? "None";

                    logger.LogError(
                        "Circuit breaker OPENED for {BreakDelay}s after {FailureThreshold} consecutive failures. " +
                        "Last failure - Status: {StatusCode}, Exception: {ExceptionType}, Request: {RequestUri}",
                        breakDelay.TotalSeconds,
                        failureThreshold,
                        statusCode,
                        exceptionType,
                        outcome.Result?.RequestMessage?.RequestUri);
                },
                onReset: () =>
                {
                    logger.LogInformation(
                        "Circuit breaker RESET - Normal operations resumed");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation(
                        "Circuit breaker HALF-OPEN - Testing service availability with next request");
                });
    }
}

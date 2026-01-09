using TentMan.ApiClient.Authentication.Configuration;
using TentMan.ApiClient.Authentication.Handlers;
using TentMan.ApiClient.Authentication.Providers;
using TentMan.ApiClient.Authentication.Services;
using TentMan.ApiClient.Authentication.Storage;
using TentMan.ApiClient.Configuration;
using TentMan.ApiClient.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace TentMan.ApiClient.Extensions;

/// <summary>
/// Extension methods for configuring API client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the API client services with authentication for Blazor WebAssembly applications.
    /// Uses singleton lifetime for token storage (single-user context).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureAuthentication">Optional action to configure authentication.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClientForWasm(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuthenticationOptions>? configureAuthentication = null)
    {
        return AddApiClient(services, configuration, configureAuthentication, isWasm: true);
    }

    /// <summary>
    /// Adds the API client services with authentication for Blazor Server applications.
    /// Uses scoped lifetime for token storage (per-user/per-circuit isolation).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureAuthentication">Optional action to configure authentication.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClientForServer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuthenticationOptions>? configureAuthentication = null)
    {
        return AddApiClient(services, configuration, configureAuthentication, isWasm: false);
    }

    /// <summary>
    /// Adds the API client services to the service collection with custom configuration for WASM.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the API client options.</param>
    /// <param name="configureAuthentication">Optional action to configure authentication.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClientForWasm(
        this IServiceCollection services,
        Action<ApiClientOptions> configureOptions,
        Action<AuthenticationOptions>? configureAuthentication = null)
    {
        return AddApiClient(services, configureOptions, configureAuthentication, isWasm: true);
    }

    /// <summary>
    /// Adds the API client services to the service collection with custom configuration for Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the API client options.</param>
    /// <param name="configureAuthentication">Optional action to configure authentication.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClientForServer(
        this IServiceCollection services,
        Action<ApiClientOptions> configureOptions,
        Action<AuthenticationOptions>? configureAuthentication = null)
    {
        return AddApiClient(services, configureOptions, configureAuthentication, isWasm: false);
    }

    /// <summary>
    /// Core method to add API client with configuration.
    /// </summary>
    private static IServiceCollection AddApiClient(
        IServiceCollection services,
        IConfiguration configuration,
        Action<AuthenticationOptions>? configureAuthentication,
        bool isWasm)
    {
        // Bind configuration
        services.Configure<ApiClientOptions>(
            configuration.GetSection(ApiClientOptions.SectionName));

        var options = configuration
            .GetSection(ApiClientOptions.SectionName)
            .Get<ApiClientOptions>() ?? new ApiClientOptions();

        var authOptions = ConfigureAuthentication(services, configuration, configureAuthentication);

        RegisterAuthenticationServices(services, authOptions, isWasm);
        ConfigureHttpClients(services, options, authOptions);

        return services;
    }

    /// <summary>
    /// Core method to add API client with custom configuration.
    /// </summary>
    private static IServiceCollection AddApiClient(
        IServiceCollection services,
        Action<ApiClientOptions> configureOptions,
        Action<AuthenticationOptions>? configureAuthentication,
        bool isWasm)
    {
        services.Configure(configureOptions);

        var options = new ApiClientOptions();
        configureOptions(options);

        var authOptions = new AuthenticationOptions();
        if (configureAuthentication != null)
        {
            services.Configure(configureAuthentication);
            configureAuthentication(authOptions);
        }

        RegisterAuthenticationServices(services, authOptions, isWasm);
        ConfigureHttpClients(services, options, authOptions);

        return services;
    }

    /// <summary>
    /// Configures authentication options.
    /// </summary>
    private static AuthenticationOptions ConfigureAuthentication(
        IServiceCollection services,
        IConfiguration configuration,
        Action<AuthenticationOptions>? configureAuthentication)
    {
        var authOptions = new AuthenticationOptions();

        if (configureAuthentication != null)
        {
            services.Configure(configureAuthentication);
            configureAuthentication(authOptions);
        }
        else
        {
            services.Configure<AuthenticationOptions>(
                configuration.GetSection(AuthenticationOptions.SectionName));
            authOptions = configuration
                .GetSection(AuthenticationOptions.SectionName)
                .Get<AuthenticationOptions>() ?? new AuthenticationOptions();
        }

        return authOptions;
    }

    /// <summary>
    /// Registers authentication services based on hosting model.
    /// </summary>
    private static void RegisterAuthenticationServices(
        IServiceCollection services,
        AuthenticationOptions options,
        bool isWasm)
    {
        if (isWasm)
        {
            RegisterAuthenticationServicesForWasm(services, options);
        }
        else
        {
            RegisterAuthenticationServicesForServer(services, options);
        }
    }

    /// <summary>
    /// Configures the HttpClients with policies and handlers.
    /// </summary>
    private static void ConfigureHttpClients(
        IServiceCollection services,
        ApiClientOptions options,
        AuthenticationOptions authOptions)
    {
        // Configure Products API Client (requires authentication)
        ConfigureHttpClient<IProductsApiClient, ProductsApiClient>(services, options, authOptions, useAuthHandler: true);

        // Configure Buildings API Client (requires authentication)
        ConfigureHttpClient<IBuildingsApiClient, BuildingsApiClient>(services, options, authOptions, useAuthHandler: true);

        // Configure Owners API Client (requires authentication)
        ConfigureHttpClient<IOwnersApiClient, OwnersApiClient>(services, options, authOptions, useAuthHandler: true);

        // Configure Tenants API Client (requires authentication)
        ConfigureHttpClient<ITenantsApiClient, TenantsApiClient>(services, options, authOptions, useAuthHandler: true);

        // Configure Leases API Client (requires authentication)
        ConfigureHttpClient<ILeasesApiClient, LeasesApiClient>(services, options, authOptions, useAuthHandler: true);

        // Configure Authentication API Client
        // The implementation will handle token attachment internally based on endpoint
        ConfigureHttpClient<IAuthenticationApiClient, AuthenticationApiClient>(services, options, authOptions, useAuthHandler: true);
    }

    /// <summary>
    /// Configures an individual HTTP client.
    /// </summary>
    private static void ConfigureHttpClient<TInterface, TImplementation>(
        IServiceCollection services,
        ApiClientOptions options,
        AuthenticationOptions authOptions,
        bool useAuthHandler = true)
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

        // Add authentication handler if enabled and requested
        // For AuthenticationApiClient, the handler will intelligently skip token attachment
        // for unauthenticated endpoints (login/register/refresh/forgot-password/reset-password/confirm-email)
        if (useAuthHandler && authOptions.AutoAttachToken)
        {
            httpClientBuilder.AddHttpMessageHandler<AuthenticationMessageHandler>();
        }
    }

    /// <summary>
    /// Registers authentication services for Blazor WebAssembly with singleton token storage.
    /// </summary>
    private static void RegisterAuthenticationServicesForWasm(
        IServiceCollection services,
        AuthenticationOptions options)
    {
        // Register token storage as SINGLETON for WASM (single-user context)
        if (options.UseBrowserStorage)
        {
            services.AddSingleton<ITokenStorage, BrowserLocalTokenStorage>();
        }
        else
        {
            services.AddSingleton<ITokenStorage, InMemoryTokenStorage>();
        }

        // Register core authentication services
        services.AddScoped<ITokenManager, TokenManager>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddTransient<AuthenticationMessageHandler>();

        // Register authentication state provider for Blazor
        services.AddScoped<ApiAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<ApiAuthenticationStateProvider>());
    }

    /// <summary>
    /// Registers authentication services for Blazor Server with scoped token storage.
    /// </summary>
    private static void RegisterAuthenticationServicesForServer(
        IServiceCollection services,
        AuthenticationOptions options)
    {
        // Register token storage as SCOPED for Server (per-user/per-circuit isolation)
        // Note: Browser storage is not used for Server, always use in-memory
        services.AddScoped<ITokenStorage, InMemoryTokenStorage>();

        // Register core authentication services
        services.AddScoped<ITokenManager, TokenManager>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddTransient<AuthenticationMessageHandler>();

        // Register authentication state provider for Blazor
        services.AddScoped<ApiAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<ApiAuthenticationStateProvider>());
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

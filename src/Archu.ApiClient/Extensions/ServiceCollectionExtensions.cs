using Archu.ApiClient.Authentication.Configuration;
using Archu.ApiClient.Authentication.Handlers;
using Archu.ApiClient.Authentication.Providers;
using Archu.ApiClient.Authentication.Services;
using Archu.ApiClient.Authentication.Storage;
using Archu.ApiClient.Configuration;
using Archu.ApiClient.Services;
using Microsoft.AspNetCore.Components.Authorization;
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
    /// <param name="configureAuthentication">Optional action to configure authentication.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClient(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuthenticationOptions>? configureAuthentication = null)
    {
        // Bind configuration
        services.Configure<ApiClientOptions>(
            configuration.GetSection(ApiClientOptions.SectionName));

        var options = configuration
            .GetSection(ApiClientOptions.SectionName)
            .Get<ApiClientOptions>() ?? new ApiClientOptions();

        // Configure authentication if specified
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

        // Register authentication services
        RegisterAuthenticationServices(services, authOptions);

        // Add HttpClient with Polly retry policy and authentication
        var httpClientBuilder = services.AddHttpClient<IProductsApiClient, ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler((serviceProvider, request) =>
            GetRetryPolicy(options.RetryCount, serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()))
        .AddPolicyHandler((serviceProvider, request) =>
            GetCircuitBreakerPolicy(serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()));

        // Add authentication handler if enabled
        if (authOptions.AutoAttachToken)
        {
            httpClientBuilder.AddHttpMessageHandler<AuthenticationMessageHandler>();
        }

        return services;
    }

    /// <summary>
    /// Adds the API client services to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the API client options.</param>
    /// <param name="configureAuthentication">Optional action to configure authentication.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiClient(
        this IServiceCollection services,
        Action<ApiClientOptions> configureOptions,
        Action<AuthenticationOptions>? configureAuthentication = null)
    {
        services.Configure(configureOptions);

        var options = new ApiClientOptions();
        configureOptions(options);

        // Configure authentication if specified
        var authOptions = new AuthenticationOptions();
        if (configureAuthentication != null)
        {
            services.Configure(configureAuthentication);
            configureAuthentication(authOptions);
        }

        // Register authentication services
        RegisterAuthenticationServices(services, authOptions);

        var httpClientBuilder = services.AddHttpClient<IProductsApiClient, ProductsApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler((serviceProvider, request) =>
            GetRetryPolicy(options.RetryCount, serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()))
        .AddPolicyHandler((serviceProvider, request) =>
            GetCircuitBreakerPolicy(serviceProvider.GetRequiredService<ILogger<ProductsApiClient>>()));

        // Add authentication handler if enabled
        if (authOptions.AutoAttachToken)
        {
            httpClientBuilder.AddHttpMessageHandler<AuthenticationMessageHandler>();
        }

        return services;
    }

    /// <summary>
    /// Registers authentication services.
    /// </summary>
    private static void RegisterAuthenticationServices(
        IServiceCollection services,
        AuthenticationOptions options)
    {
        // Register token storage based on configuration
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

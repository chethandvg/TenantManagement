namespace TentMan.AdminApiClient.Configuration;

/// <summary>
/// Configuration options for the Admin API client.
/// </summary>
public sealed class AdminApiClientOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "AdminApiClient";

    /// <summary>
    /// Gets or sets the base URL of the Admin API.
    /// </summary>
    /// <example>https://localhost:7002</example>
    public string BaseUrl { get; set; } = "https://localhost:7002";

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    /// <remarks>
    /// Default is 30 seconds. Increase for long-running operations.
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed requests.
    /// </summary>
    /// <remarks>
    /// Default is 3 retries. Applies only to retryable errors (5xx, network issues).
    /// </remarks>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the API version to use.
    /// </summary>
    /// <example>v1</example>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging.
    /// </summary>
    /// <remarks>
    /// When enabled, logs all HTTP requests/responses including headers and bodies.
    /// Disable in production to avoid logging sensitive data.
    /// </remarks>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// </summary>
    /// <remarks>
    /// Number of consecutive failures before opening the circuit breaker.
    /// Default is 5 failures.
    /// </remarks>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker duration in seconds.
    /// </summary>
    /// <remarks>
    /// Duration to keep the circuit breaker open before attempting recovery.
    /// Default is 30 seconds.
    /// </remarks>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff retry policy in seconds.
    /// </summary>
    /// <remarks>
    /// The actual delay for each retry is calculated as: baseDelay * (2 ^ retryAttempt).
    /// Default is 1 second.
    /// </remarks>
    public double RetryBaseDelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets a value indicating whether the circuit breaker is enabled.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the retry policy is enabled.
    /// </summary>
    public bool EnableRetryPolicy { get; set; } = true;
}

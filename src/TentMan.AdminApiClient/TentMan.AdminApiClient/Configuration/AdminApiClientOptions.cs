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
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of retry attempts for failed requests.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the API version.
    /// </summary>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>
    /// Gets or sets whether to enable detailed logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of failures before the circuit breaker opens.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the duration in seconds that the circuit breaker stays open.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the base delay in seconds for exponential backoff retry.
    /// </summary>
    public double RetryBaseDelaySeconds { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether to enable the circuit breaker policy.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable retry policies.
    /// </summary>
    public bool EnableRetryPolicy { get; set; } = true;
}

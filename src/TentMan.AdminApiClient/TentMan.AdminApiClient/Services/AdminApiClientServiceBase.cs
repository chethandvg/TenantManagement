using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TentMan.AdminApiClient.Configuration;
using TentMan.AdminApiClient.Exceptions;
using TentMan.Contracts.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TentMan.AdminApiClient.Services;

/// <summary>
/// Base class for Admin API client services providing common HTTP operations.
/// </summary>
public abstract class AdminApiClientServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger _logger;
    private readonly string _apiVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminApiClientServiceBase"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="options">The Admin API client options.</param>
    /// <param name="logger">The logger instance.</param>
    protected AdminApiClientServiceBase(HttpClient httpClient, IOptions<AdminApiClientOptions> options, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);
        _apiVersion = options.Value.ApiVersion;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Gets the endpoint name for this API client (e.g., "users", "roles").
    /// This is combined with the configured API version to form the full base path.
    /// </summary>
    protected abstract string EndpointName { get; }

    /// <summary>
    /// Gets the base path for the API endpoint, constructed from the configured API version.
    /// </summary>
    protected string BasePath => $"api/{_apiVersion}/admin/{EndpointName}";

    /// <summary>
    /// Sends a GET request and returns the response data.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response data.</typeparam>
    /// <param name="endpoint">The endpoint path relative to the base path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API response containing the requested data.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown when the resource is not found (404).</exception>
    /// <exception cref="AuthorizationException">Thrown when authorization fails (401/403).</exception>
    /// <exception cref="ServerException">Thrown when a server error occurs (5xx).</exception>
    /// <exception cref="NetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="AdminApiClientException">Thrown for other API errors.</exception>
    protected async Task<ApiResponse<TResponse>> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(endpoint);
        _logger.LogDebug("Sending GET request to {Uri}", uri);

        try
        {
            var response = await _httpClient.GetAsync(uri, cancellationToken);

            _logger.LogDebug(
                "GET request to {Uri} completed with status {StatusCode}",
                uri,
                (int)response.StatusCode);

            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not AdminApiClientException)
        {
            _logger.LogError(ex, "Error during GET request to {Uri}", uri);
            return HandleException<TResponse>(ex);
        }
    }

    /// <summary>
    /// Sends a POST request with a request body and returns the response data.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request data.</typeparam>
    /// <typeparam name="TResponse">The type of the response data.</typeparam>
    /// <param name="endpoint">The endpoint path relative to the base path.</param>
    /// <param name="request">The request data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API response containing the created resource.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails (400).</exception>
    /// <exception cref="AuthorizationException">Thrown when authorization fails (401/403).</exception>
    /// <exception cref="ServerException">Thrown when a server error occurs (5xx).</exception>
    /// <exception cref="NetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="AdminApiClientException">Thrown for other API errors.</exception>
    protected async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(endpoint);
        _logger.LogDebug("Sending POST request to {Uri}", uri);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                uri,
                request,
                _jsonOptions,
                cancellationToken);

            _logger.LogDebug(
                "POST request to {Uri} completed with status {StatusCode}",
                uri,
                (int)response.StatusCode);

            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not AdminApiClientException)
        {
            _logger.LogError(ex, "Error during POST request to {Uri}", uri);
            return HandleException<TResponse>(ex);
        }
    }

    /// <summary>
    /// Sends a DELETE request and returns the response.
    /// </summary>
    /// <param name="endpoint">The endpoint path relative to the base path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API response indicating success or failure.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown when the resource is not found (404).</exception>
    /// <exception cref="AuthorizationException">Thrown when authorization fails (401/403).</exception>
    /// <exception cref="ServerException">Thrown when a server error occurs (5xx).</exception>
    /// <exception cref="NetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="AdminApiClientException">Thrown for other API errors.</exception>
    protected async Task<ApiResponse<bool>> DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(endpoint);
        _logger.LogDebug("Sending DELETE request to {Uri}", uri);

        try
        {
            var response = await _httpClient.DeleteAsync(uri, cancellationToken);

            _logger.LogDebug(
                "DELETE request to {Uri} completed with status {StatusCode}",
                uri,
                (int)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Resource deleted successfully at {Uri}", uri);
                return ApiResponse<bool>.Ok(true, "Resource deleted successfully");
            }

            await ThrowForStatusCodeAsync(response, cancellationToken);

            // This line won't be reached due to the throw above, but keeps the compiler happy
            return ApiResponse<bool>.Fail("Request failed");
        }
        catch (Exception ex) when (ex is not AdminApiClientException)
        {
            _logger.LogError(ex, "Error during DELETE request to {Uri}", uri);
            return HandleException<bool>(ex);
        }
    }

    /// <summary>
    /// Builds a complete URI from the base path and endpoint.
    /// </summary>
    private string BuildUri(string endpoint)
    {
        var trimmedEndpoint = endpoint.TrimStart('/');

        // If endpoint starts with query string, don't add separator slash
        if (trimmedEndpoint.StartsWith('?'))
        {
            return $"{BasePath}{trimmedEndpoint}";
        }

        return string.IsNullOrWhiteSpace(trimmedEndpoint)
            ? BasePath
            : $"{BasePath}/{trimmedEndpoint}";
    }

    /// <summary>
    /// Processes the HTTP response and deserializes the content.
    /// </summary>
    private async Task<ApiResponse<T>> ProcessResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
                    _jsonOptions,
                    cancellationToken);

                if (apiResponse == null)
                {
                    _logger.LogWarning(
                        "Received null response from server for {RequestUri}",
                        response.RequestMessage?.RequestUri);
                    return ApiResponse<T>.Fail("Received null response from server");
                }

                _logger.LogDebug(
                    "Successfully deserialized response from {RequestUri}. Success: {Success}",
                    response.RequestMessage?.RequestUri,
                    apiResponse.Success);

                return apiResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to deserialize response from {RequestUri}. StatusCode: {StatusCode}",
                    response.RequestMessage?.RequestUri,
                    (int)response.StatusCode);

                throw new AdminApiClientException(
                    "Failed to deserialize response from server",
                    (int)response.StatusCode,
                    ex);
            }
        }

        await ThrowForStatusCodeAsync(response, cancellationToken);

        // This line won't be reached due to the throw above, but keeps the compiler happy
        return ApiResponse<T>.Fail("Request failed");
    }

    /// <summary>
    /// Throws an appropriate exception based on the HTTP status code.
    /// </summary>
    private async Task ThrowForStatusCodeAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var requestUri = response.RequestMessage?.RequestUri;
        string errorContent;

        try
        {
            errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read error response content from {RequestUri}", requestUri);
            errorContent = "Unable to read error response";
        }

        var errorMessage = $"Request failed with status {statusCode} ({response.StatusCode})";

        // Try to parse as ApiResponse to get structured errors
        IEnumerable<string>? errors = null;
        if (!string.IsNullOrWhiteSpace(errorContent))
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(errorContent, _jsonOptions);

                if (errorResponse?.Errors != null)
                {
                    errors = errorResponse.Errors;
                }
                else if (!string.IsNullOrWhiteSpace(errorResponse?.Message))
                {
                    errorMessage = errorResponse.Message;
                }
            }
            catch
            {
                // If we can't parse as ApiResponse, use raw content
                errors = new[] { errorContent };
            }
        }

        // Log the error before throwing
        _logger.LogWarning(
            "Request to {RequestUri} failed with status {StatusCode}. Message: {ErrorMessage}, Errors: {Errors}",
            requestUri,
            statusCode,
            errorMessage,
            errors != null ? string.Join(", ", errors) : "None");

        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ValidationException(errorMessage, errors),
            HttpStatusCode.Unauthorized => new AuthorizationException(errorMessage, 401),
            HttpStatusCode.Forbidden => new AuthorizationException(errorMessage, 403),
            HttpStatusCode.NotFound => new ResourceNotFoundException(errorMessage),
            HttpStatusCode.Conflict => new AdminApiClientException(errorMessage, statusCode, errors),
            HttpStatusCode.UnprocessableEntity => new ValidationException(errorMessage, errors),
            HttpStatusCode.TooManyRequests => new AdminApiClientException("Rate limit exceeded", statusCode, errors),
            >= HttpStatusCode.InternalServerError => new ServerException(errorMessage, statusCode, errors),
            _ => new AdminApiClientException(errorMessage, statusCode, errors)
        };
    }

    /// <summary>
    /// Handles exceptions and returns a failed API response.
    /// </summary>
    private ApiResponse<T> HandleException<T>(Exception ex)
    {
        if (ex is HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Network error occurred during HTTP request");
            return ApiResponse<T>.Fail(
                "Network error occurred while processing the request",
                new[] { httpEx.Message });
        }

        if (ex is TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Request was cancelled or timed out");
            return ApiResponse<T>.Fail(
                "Request was cancelled or timed out",
                new[] { ex.Message });
        }

        _logger.LogError(ex, "Unexpected error occurred during HTTP request");
        return ApiResponse<T>.Fail(
            "An unexpected error occurred while processing the request",
            new[] { ex.Message });
    }
}

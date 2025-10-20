using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Archu.ApiClient.Exceptions;
using Archu.Contracts.Common;

namespace Archu.ApiClient.Services;

/// <summary>
/// Base class for API client services providing common HTTP operations.
/// </summary>
public abstract class ApiClientServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientServiceBase"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    protected ApiClientServiceBase(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Gets the base path for the API endpoint (e.g., "products", "users").
    /// </summary>
    protected abstract string BasePath { get; }

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
    /// <exception cref="ApiClientException">Thrown for other API errors.</exception>
    protected async Task<ApiResponse<TResponse>> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                BuildUri(endpoint),
                cancellationToken);

            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not ApiClientException)
        {
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
    /// <exception cref="ApiClientException">Thrown for other API errors.</exception>
    protected async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                BuildUri(endpoint),
                request,
                _jsonOptions,
                cancellationToken);

            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not ApiClientException)
        {
            return HandleException<TResponse>(ex);
        }
    }

    /// <summary>
    /// Sends a PUT request with a request body and returns the response data.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request data.</typeparam>
    /// <typeparam name="TResponse">The type of the response data.</typeparam>
    /// <param name="endpoint">The endpoint path relative to the base path.</param>
    /// <param name="request">The request data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API response containing the updated resource.</returns>
    /// <exception cref="ResourceNotFoundException">Thrown when the resource is not found (404).</exception>
    /// <exception cref="ValidationException">Thrown when validation fails (400).</exception>
    /// <exception cref="AuthorizationException">Thrown when authorization fails (401/403).</exception>
    /// <exception cref="ServerException">Thrown when a server error occurs (5xx).</exception>
    /// <exception cref="NetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="ApiClientException">Thrown for other API errors.</exception>
    protected async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
        String endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                BuildUri(endpoint),
                request,
                _jsonOptions,
                cancellationToken);

            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not ApiClientException)
        {
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
    /// <exception cref="ApiClientException">Thrown for other API errors.</exception>
    protected async Task<ApiResponse<bool>> DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                BuildUri(endpoint),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ApiResponse<bool>.Ok(true, "Resource deleted successfully");
            }

            await ThrowForStatusCodeAsync(response, cancellationToken);

            // This line won't be reached due to the throw above, but keeps the compiler happy
            return ApiResponse<bool>.Fail("Request failed");
        }
        catch (Exception ex) when (ex is not ApiClientException)
        {
            return HandleException<bool>(ex);
        }
    }

    /// <summary>
    /// Builds a complete URI from the base path and endpoint.
    /// </summary>
    private string BuildUri(string endpoint)
    {
        var trimmedEndpoint = endpoint.TrimStart('/');
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

                return apiResponse ?? ApiResponse<T>.Fail("Received null response from server");
            }
            catch (JsonException ex)
            {
                throw new ApiClientException(
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
        string errorContent;

        try
        {
            errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            errorContent = "Unable to read error response";
        }

        var errorMessage = $"Request failed with status {statusCode} ({response.StatusCode})";

        // Try to parse as ApiResponse to get structured errors
        IEnumerable<string>? errors = null;
        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(
                _jsonOptions,
                cancellationToken);

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
            if (!string.IsNullOrWhiteSpace(errorContent))
            {
                errors = new[] { errorContent };
            }
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ValidationException(errorMessage, errors),
            HttpStatusCode.Unauthorized => new AuthorizationException(errorMessage, 401),
            HttpStatusCode.Forbidden => new AuthorizationException(errorMessage, 403),
            HttpStatusCode.NotFound => new ResourceNotFoundException(errorMessage),
            HttpStatusCode.Conflict => new ApiClientException(errorMessage, statusCode, errors),
            HttpStatusCode.UnprocessableEntity => new ValidationException(errorMessage, errors),
            HttpStatusCode.TooManyRequests => new ApiClientException("Rate limit exceeded", statusCode, errors),
            >= HttpStatusCode.InternalServerError => new ServerException(errorMessage, statusCode, errors),
            _ => new ApiClientException(errorMessage, statusCode, errors)
        };
    }

    /// <summary>
    /// Handles exceptions and returns a failed API response.
    /// </summary>
    private static ApiResponse<T> HandleException<T>(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpEx => ApiResponse<T>.Fail(
                "Network error occurred while processing the request",
                new[] { httpEx.Message }),

            TaskCanceledException or OperationCanceledException => ApiResponse<T>.Fail(
                "Request was cancelled or timed out",
                new[] { ex.Message }),

            _ => ApiResponse<T>.Fail(
                "An unexpected error occurred while processing the request",
                new[] { ex.Message })
        };
    }
}

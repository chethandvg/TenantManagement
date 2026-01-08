using TentMan.ApiClient.Exceptions;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Helpers;

/// <summary>
/// Helper class for handling API client exceptions.
/// </summary>
public static class ExceptionHandler
{
    /// <summary>
    /// Handles an API client exception and logs it appropriately.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operation">Description of the operation that failed.</param>
    public static void HandleException(Exception exception, ILogger logger, string operation)
    {
        switch (exception)
        {
            case ResourceNotFoundException notFoundEx:
                logger.LogWarning(
                    notFoundEx,
                    "Resource not found during {Operation}. Resource: {ResourceType}, ID: {ResourceId}",
                    operation,
                    notFoundEx.ResourceType,
                    notFoundEx.ResourceId);
                break;

            case ValidationException validationEx:
                logger.LogWarning(
                    validationEx,
                    "Validation failed during {Operation}. Errors: {Errors}",
                    operation,
                    string.Join(", ", validationEx.Errors));
                break;

            case AuthorizationException authEx:
                logger.LogWarning(
                    authEx,
                    "Authorization failed during {Operation}. Status: {StatusCode}",
                    operation,
                    authEx.StatusCode);
                break;

            case ServerException serverEx:
                logger.LogError(
                    serverEx,
                    "Server error occurred during {Operation}. Status: {StatusCode}, Errors: {Errors}",
                    operation,
                    serverEx.StatusCode,
                    string.Join(", ", serverEx.Errors));
                break;

            case NetworkException networkEx:
                logger.LogError(
                    networkEx,
                    "Network error occurred during {Operation}",
                    operation);
                break;

            case ApiClientException apiEx:
                logger.LogError(
                    apiEx,
                    "API error occurred during {Operation}. Status: {StatusCode}, Errors: {Errors}",
                    operation,
                    apiEx.StatusCode,
                    string.Join(", ", apiEx.Errors));
                break;

            default:
                logger.LogError(
                    exception,
                    "Unexpected error occurred during {Operation}",
                    operation);
                break;
        }
    }

    /// <summary>
    /// Gets a user-friendly error message from an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A user-friendly error message.</returns>
    public static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            ResourceNotFoundException => "The requested resource was not found.",
            ValidationException validationEx => $"Validation failed: {string.Join(", ", validationEx.Errors.Take(3))}",
            AuthorizationException authEx when authEx.StatusCode == 401 => "You are not authenticated. Please log in.",
            AuthorizationException => "You do not have permission to perform this action.",
            ServerException => "A server error occurred. Please try again later.",
            NetworkException => "A network error occurred. Please check your connection.",
            TaskCanceledException or OperationCanceledException => "The request timed out. Please try again.",
            ApiClientException apiEx => apiEx.Message,
            _ => "An unexpected error occurred. Please try again."
        };
    }

    /// <summary>
    /// Determines if an exception is retryable.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is retryable, false otherwise.</returns>
    public static bool IsRetryable(Exception exception)
    {
        return exception switch
        {
            NetworkException => true,
            ServerException serverEx when serverEx.StatusCode >= 500 && serverEx.StatusCode != 501 => true,
            ApiClientException apiEx when apiEx.StatusCode == 429 => true, // Rate limited
            HttpRequestException => true,
            TaskCanceledException => true,
            _ => false
        };
    }
}

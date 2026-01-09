using TentMan.AdminApiClient.Exceptions;
using Microsoft.Extensions.Logging;

namespace TentMan.AdminApiClient.Helpers;

/// <summary>
/// Helper class for handling exceptions in Admin API client operations.
/// </summary>
public static class ExceptionHandler
{
    /// <summary>
    /// Handles an exception and logs it appropriately.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The name of the operation that failed.</param>
    public static void HandleException(Exception exception, ILogger logger, string operationName)
    {
        switch (exception)
        {
            case ValidationException validationEx:
                logger.LogWarning(
                    validationEx,
                    "Validation error in {Operation}: {Message}",
                    operationName,
                    validationEx.Message);
                break;

            case AuthorizationException authEx:
                logger.LogWarning(
                    authEx,
                    "Authorization error in {Operation}: {Message}",
                    operationName,
                    authEx.Message);
                break;

            case ResourceNotFoundException notFoundEx:
                logger.LogWarning(
                    notFoundEx,
                    "Resource not found in {Operation}: {Message}",
                    operationName,
                    notFoundEx.Message);
                break;

            case ServerException serverEx:
                logger.LogError(
                    serverEx,
                    "Server error in {Operation}: {Message}",
                    operationName,
                    serverEx.Message);
                break;

            case NetworkException networkEx:
                logger.LogError(
                    networkEx,
                    "Network error in {Operation}: {Message}",
                    operationName,
                    networkEx.Message);
                break;

            case AdminApiClientException apiEx:
                logger.LogError(
                    apiEx,
                    "API error in {Operation}: {Message}",
                    operationName,
                    apiEx.Message);
                break;

            default:
                logger.LogError(
                    exception,
                    "Unexpected error in {Operation}: {Message}",
                    operationName,
                    exception.Message);
                break;
        }
    }
}

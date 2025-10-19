using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using Archu.Contracts.Common;

namespace Archu.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches and formats exceptions.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, details, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred",
                "Validation failed",
                validationEx.Errors.Select(e => e.ErrorMessage).ToList()
            ),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "The resource was modified by another user. Please reload and try again.",
                "Concurrency conflict occurred",
                (IEnumerable<string>?)null
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "The requested resource was not found.",
                exception.Message,
                (IEnumerable<string>?)null
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "You do not have permission to access this resource.",
                exception.Message,
                (IEnumerable<string>?)null
            ),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request parameters.",
                exception.Message,
                (IEnumerable<string>?)null
            ),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "The operation is invalid in the current state.",
                exception.Message,
                (IEnumerable<string>?)null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request.",
                exception.Message,
                (IEnumerable<string>?)null
            )
        };

        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        // Build error details for development environment
        var errorDetails = new List<string>();
        if (_environment.IsDevelopment())
        {
            if (!string.IsNullOrWhiteSpace(details))
            {
                errorDetails.Add($"Details: {details}");
            }
            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                errorDetails.Add($"StackTrace: {exception.StackTrace}");
            }
            errorDetails.Add($"TraceId: {context.TraceIdentifier}");
        }

        // Combine validation errors with development details
        var allErrors = errors != null
            ? errors.Concat(errorDetails)
            : errorDetails.Any() ? errorDetails : null;

        // Use ApiResponse<object> for consistent error contract
        var response = ApiResponse<object>.Fail(
            message: message,
            errors: allErrors
        );

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(response, options);
    }
}

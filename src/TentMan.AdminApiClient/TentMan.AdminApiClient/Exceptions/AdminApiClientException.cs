namespace TentMan.AdminApiClient.Exceptions;

/// <summary>
/// Base exception for Admin API client errors.
/// </summary>
public class AdminApiClientException : Exception
{
    /// <summary>
    /// Gets the HTTP status code associated with this exception.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the collection of error messages.
    /// </summary>
    public IEnumerable<string>? Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminApiClientException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errors">Optional collection of error messages.</param>
    public AdminApiClientException(string message, int statusCode = 0, IEnumerable<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminApiClientException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="innerException">The inner exception.</param>
    public AdminApiClientException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

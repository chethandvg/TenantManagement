namespace Archu.ApiClient.Exceptions;

/// <summary>
/// Base exception for all API client errors.
/// </summary>
public class ApiClientException : Exception
{
    /// <summary>
    /// Gets the HTTP status code associated with the error, if any.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Gets the collection of error messages.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientException"/> class.
    /// </summary>
    public ApiClientException(string message)
        : base(message)
    {
        Errors = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientException"/> class.
    /// </summary>
    public ApiClientException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientException"/> class.
    /// </summary>
    public ApiClientException(string message, int statusCode, IEnumerable<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClientException"/> class.
    /// </summary>
    public ApiClientException(string message, int statusCode, Exception innerException, IEnumerable<string>? errors = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Errors = errors?.ToList() ?? new List<string>();
    }
}

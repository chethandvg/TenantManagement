namespace TentMan.AdminApiClient.Exceptions;

/// <summary>
/// Exception thrown when a server error occurs (5xx status codes).
/// </summary>
public class ServerException : AdminApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errors">Optional collection of error messages.</param>
    public ServerException(string message, int statusCode, IEnumerable<string>? errors = null)
        : base(message, statusCode, errors)
    {
    }
}

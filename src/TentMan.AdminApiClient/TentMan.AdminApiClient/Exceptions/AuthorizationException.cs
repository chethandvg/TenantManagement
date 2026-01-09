namespace TentMan.AdminApiClient.Exceptions;

/// <summary>
/// Exception thrown when authorization fails (401 Unauthorized or 403 Forbidden).
/// </summary>
public class AuthorizationException : AdminApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code (401 or 403).</param>
    public AuthorizationException(string message, int statusCode)
        : base(message, statusCode)
    {
    }
}

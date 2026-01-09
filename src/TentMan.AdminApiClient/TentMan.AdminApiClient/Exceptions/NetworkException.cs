namespace TentMan.AdminApiClient.Exceptions;

/// <summary>
/// Exception thrown when a network error occurs during an HTTP request.
/// </summary>
public class NetworkException : AdminApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NetworkException(string message, Exception innerException)
        : base(message, 0, innerException)
    {
    }
}

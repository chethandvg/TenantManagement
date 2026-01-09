namespace TentMan.AdminApiClient.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found (404 Not Found).
/// </summary>
public class ResourceNotFoundException : AdminApiClientException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ResourceNotFoundException(string message)
        : base(message, 404)
    {
    }
}

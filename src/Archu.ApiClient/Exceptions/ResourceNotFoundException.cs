namespace Archu.ApiClient.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found (HTTP 404).
/// </summary>
public sealed class ResourceNotFoundException : ApiClientException
{
    /// <summary>
    /// Gets the type of resource that was not found.
    /// </summary>
    public string? ResourceType { get; }

    /// <summary>
    /// Gets the identifier of the resource that was not found.
    /// </summary>
    public object? ResourceId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    public ResourceNotFoundException(string message)
        : base(message, 404)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    public ResourceNotFoundException(string resourceType, object resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found.", 404)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    public ResourceNotFoundException(string message, Exception innerException)
        : base(message, 404, innerException)
    {
    }
}

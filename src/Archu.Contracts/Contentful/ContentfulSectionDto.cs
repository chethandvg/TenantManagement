namespace Archu.Contracts.Contentful;

/// <summary>
/// Data transfer object for Contentful section/component.
/// </summary>
public record ContentfulSectionDto
{
    /// <summary>
    /// Gets the section identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the content type ID of this section.
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the fields of this section.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Fields { get; init; } = new Dictionary<string, object?>();
}

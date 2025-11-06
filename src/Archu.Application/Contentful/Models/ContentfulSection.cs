namespace Archu.Application.Contentful.Models;

/// <summary>
/// Represents a dynamic section/component in a Contentful page.
/// This is a flexible model that can represent different section types.
/// </summary>
public class ContentfulSection
{
    /// <summary>
    /// Gets or sets the section identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type ID of this section.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fields of this section as a dictionary.
    /// This allows for flexible section types with varying fields.
    /// </summary>
    public Dictionary<string, object?> Fields { get; set; } = new();

    /// <summary>
    /// Gets or sets the Contentful system metadata for this section.
    /// </summary>
    public ContentfulSystemMetadata? Sys { get; set; }
}

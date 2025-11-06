namespace Archu.Application.Contentful.Models;

/// <summary>
/// Represents a Contentful page with dynamic sections.
/// </summary>
public class ContentfulPage
{
    /// <summary>
    /// Gets or sets the unique slug/URL identifier for the page.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page description/meta description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of dynamic sections/components on the page.
    /// </summary>
    public List<ContentfulSection> Sections { get; set; } = new();

    /// <summary>
    /// Gets or sets the Contentful system metadata.
    /// </summary>
    public ContentfulSystemMetadata? Sys { get; set; }
}

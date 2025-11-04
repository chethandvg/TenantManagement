namespace Archu.Contracts.Contentful;

/// <summary>
/// Data transfer object for Contentful page response.
/// </summary>
public record ContentfulPageDto
{
    /// <summary>
    /// Gets the unique slug/URL identifier for the page.
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Gets the page title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the page description/meta description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the list of dynamic sections/components on the page.
    /// </summary>
    public IReadOnlyList<ContentfulSectionDto> Sections { get; init; } = Array.Empty<ContentfulSectionDto>();
}

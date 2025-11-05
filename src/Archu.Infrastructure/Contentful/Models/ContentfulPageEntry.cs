namespace Archu.Infrastructure.Contentful.Models;

/// <summary>
/// Strongly-typed model for Contentful Page entries.
/// Field names must match Contentful field IDs (case-insensitive).
/// </summary>
public class ContentfulPageEntry
{
    public string? Slug { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<object>? Sections { get; set; }
}

using Newtonsoft.Json.Linq;

namespace Archu.Infrastructure.Contentful.Models;

/// <summary>
/// Strongly-typed model for Contentful Page entries.
/// Field names must match Contentful field IDs (case-insensitive).
/// Uses JObject for flexible field mapping to support dynamic Contentful schemas.
/// </summary>
public class ContentfulPageEntry
{
    public string? Slug { get; set; }
    public string? InternalName { get; set; }
    public string? PageName { get; set; }
    public List<JObject>? TopSection { get; set; }
    
    // Legacy fields for backward compatibility
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<JObject>? Sections { get; set; }
}

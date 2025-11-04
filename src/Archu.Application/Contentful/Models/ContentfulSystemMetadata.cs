namespace Archu.Application.Contentful.Models;

/// <summary>
/// Represents Contentful system metadata for entries.
/// </summary>
public class ContentfulSystemMetadata
{
    /// <summary>
    /// Gets or sets the entry ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the locale.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update date.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the revision number.
    /// </summary>
    public int? Revision { get; set; }
}

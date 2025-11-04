using Archu.Application.Contentful.Models;

namespace Archu.Application.Abstractions;

/// <summary>
/// Defines the contract for accessing Contentful CMS data.
/// </summary>
public interface IContentfulService
{
    /// <summary>
    /// Retrieves a page from Contentful by its URL slug.
    /// </summary>
    /// <param name="pageUrl">The page URL slug to retrieve.</param>
    /// <param name="locale">The locale/language code (e.g., "en-US", "de-DE"). If null, uses the default locale.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The Contentful page, or null if not found.</returns>
    Task<ContentfulPage?> GetPageAsync(string pageUrl, string? locale = null, CancellationToken cancellationToken = default);
}

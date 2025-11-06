using Archu.Application.Abstractions;
using Archu.Application.Contentful.Models;

namespace Archu.Infrastructure.Contentful;

/// <summary>
/// Null implementation of IContentfulService when Contentful is not configured.
/// This follows the Null Object pattern to avoid runtime exceptions during service resolution.
/// </summary>
public class NullContentfulService : IContentfulService
{
    public Task<ContentfulPage?> GetPageAsync(string pageUrl, string? locale = null, CancellationToken cancellationToken = default)
    {
        // Return null to indicate no page found
        // This is a safe default when Contentful is not configured
        return Task.FromResult<ContentfulPage?>(null);
    }
}

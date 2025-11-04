using Archu.Contracts.Contentful;
using MediatR;

namespace Archu.Application.Contentful.Queries.GetContentfulPage;

/// <summary>
/// Query to retrieve a Contentful page by its URL slug.
/// </summary>
/// <param name="PageUrl">The page URL slug.</param>
/// <param name="Locale">The locale/language code (optional).</param>
public record GetContentfulPageQuery(string PageUrl, string? Locale = null) : IRequest<ContentfulPageDto?>;

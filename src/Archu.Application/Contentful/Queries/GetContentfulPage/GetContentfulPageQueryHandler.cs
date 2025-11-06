using Archu.Application.Abstractions;
using Archu.Contracts.Contentful;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Contentful.Queries.GetContentfulPage;

/// <summary>
/// Handler for retrieving a Contentful page by URL slug.
/// </summary>
public class GetContentfulPageQueryHandler : IRequestHandler<GetContentfulPageQuery, ContentfulPageDto?>
{
    private readonly IContentfulService _contentfulService;
    private readonly ILogger<GetContentfulPageQueryHandler> _logger;

    public GetContentfulPageQueryHandler(
        IContentfulService contentfulService,
        ILogger<GetContentfulPageQueryHandler> logger)
    {
        _contentfulService = contentfulService;
        _logger = logger;
    }

    public async Task<ContentfulPageDto?> Handle(GetContentfulPageQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving Contentful page with URL: {PageUrl}, Locale: {Locale}",
            request.PageUrl,
            request.Locale ?? "default");

        var page = await _contentfulService.GetPageAsync(
            request.PageUrl,
            request.Locale,
            cancellationToken);

        if (page is null)
        {
            _logger.LogWarning("Contentful page with URL {PageUrl} not found", request.PageUrl);
            return null;
        }

        // Map to DTO
        var dto = new ContentfulPageDto
        {
            Slug = page.Slug,
            Title = page.Title,
            Description = page.Description,
            Sections = page.Sections.Select(s => new ContentfulSectionDto
            {
                Id = s.Id,
                ContentType = s.ContentType,
                Fields = s.Fields
            }).ToList()
        };

        _logger.LogInformation(
            "Successfully retrieved Contentful page {PageUrl} with {SectionCount} sections",
            request.PageUrl,
            dto.Sections.Count);

        return dto;
    }
}

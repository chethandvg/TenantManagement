using Archu.Application.Abstractions;
using Archu.Application.Contentful.Models;
using GraphQL;
using GraphQlApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Archu.Infrastructure.Contentful;

/// <summary>
/// Implementation of IContentfulService using Contentful's GraphQL API with GraphQL.Client library.
/// Provides access to Contentful CMS content through GraphQL queries with support for both
/// published and preview content.
/// </summary>
public class ContentfulService : IContentfulService
{
    private readonly IGraphQlClientService _graphQlClientService;
    private readonly ILogger<ContentfulService> _logger;

    public ContentfulService(
        IGraphQlClientService graphQlClientService,
        ILogger<ContentfulService> logger)
    {
        _graphQlClientService = graphQlClientService ?? throw new ArgumentNullException(nameof(graphQlClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ContentfulPage?> GetPageAsync(
        string pageUrl,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            throw new ArgumentException("Page URL cannot be null or empty", nameof(pageUrl));
        }

        _logger.LogInformation(
            "Fetching Contentful page with slug: {PageUrl}, locale: {Locale}",
            pageUrl,
            locale ?? "default");

        try
        {
            // Get the GraphQL client
            var graphQlClient = _graphQlClientService.GetGraphQLClient(isPreview: false);

            // Build GraphQL query using the generated query builder
            var queryBuilder = new QueryQueryBuilder()
                .WithPageCollection(
                    new PageCollectionQueryBuilder()
                        .WithAllScalarFields()
                        .WithItems(
                            new PageQueryBuilder()
                                // Explicitly specify fields to avoid _id field which causes deserialization issues
                                .WithInternalName()
                                .WithPageName()
                                .WithSlug()
                                .WithSys(new SysQueryBuilder()
                                    .WithFirstPublishedAt()
                                    .WithId()
                                    .WithLocale()
                                    .WithPublishedAt()
                                    .WithPublishedVersion()
                                    .WithSpaceId())
                                .WithTopSectionCollection(
                                    new PageTopSectionCollectionQueryBuilder()
                                        .WithLimit()
                                        .WithSkip()
                                        .WithTotal()
                                        .WithItems(
                                            new PageTopSectionItemQueryBuilder()
                                                // Union type requires fragments for each possible component type
                                                // Note: Manually specifying all fields to avoid _id field which causes deserialization issues
                                                .WithComponentCtaFragment(new ComponentCtaQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithCtaText()
                                                    .WithColorPalette()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentDuplexFragment(new ComponentDuplexQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithCtaText()
                                                    .WithColorPalette()
                                                    .WithContainerLayout()
                                                    .WithImageStyle()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentHeroBannerFragment(new ComponentHeroBannerQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithCtaText()
                                                    .WithColorPalette()
                                                    .WithHeroSize()
                                                    .WithImageStyle()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentInfoBlockFragment(new ComponentInfoBlockQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithColorPalette()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentQuoteFragment(new ComponentQuoteQueryBuilder()
                                                    .WithInternalName()
                                                    .WithQuoteAlignment()
                                                    .WithColorPalette()
                                                    .WithImagePosition()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentTextBlockFragment(new ComponentTextBlockQueryBuilder()
                                                    .WithInternalName()
                                                    .WithColorPalette()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                        )
                                )
                                .WithExtraSectionCollection(
                                    new PageExtraSectionCollectionQueryBuilder()
                                        .WithLimit()
                                        .WithSkip()
                                        .WithTotal()
                                        .WithItems(
                                            new PageExtraSectionItemQueryBuilder()
                                                // Union type requires fragments for each possible component type
                                                // Note: Manually specifying all fields to avoid _id field which causes deserialization issues
                                                .WithComponentCtaFragment(new ComponentCtaQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithCtaText()
                                                    .WithColorPalette()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentDuplexFragment(new ComponentDuplexQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithCtaText()
                                                    .WithColorPalette()
                                                    .WithContainerLayout()
                                                    .WithImageStyle()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentHeroBannerFragment(new ComponentHeroBannerQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithCtaText()
                                                    .WithColorPalette()
                                                    .WithHeroSize()
                                                    .WithImageStyle()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentInfoBlockFragment(new ComponentInfoBlockQueryBuilder()
                                                    .WithInternalName()
                                                    .WithHeadline()
                                                    .WithColorPalette()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentQuoteFragment(new ComponentQuoteQueryBuilder()
                                                    .WithInternalName()
                                                    .WithQuoteAlignment()
                                                    .WithColorPalette()
                                                    .WithImagePosition()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                                .WithComponentTextBlockFragment(new ComponentTextBlockQueryBuilder()
                                                    .WithInternalName()
                                                    .WithColorPalette()
                                                    .WithSys(new SysQueryBuilder()
                                                        .WithFirstPublishedAt()
                                                        .WithId()
                                                        .WithLocale()
                                                        .WithPublishedAt()
                                                        .WithPublishedVersion()
                                                        .WithSpaceId()))
                                        )
                                )
                                .WithSeo(new SeoQueryBuilder().WithAllScalarFields())
                                .WithPageContent(new PagePageContentQueryBuilder().WithAllFields())
                        ),
                    limit: 1,
                    locale: string.IsNullOrWhiteSpace(locale) ? null : locale,
                    where: new PageFilter { Slug = pageUrl }
                );

            var queryString = queryBuilder.Build();
            _logger.LogDebug("GraphQL Query: {Query}", queryString);

            // Create GraphQL request using GraphQL.Client library
            var request = new GraphQLRequest
            {
                Query = queryString
            };

            GraphQL.GraphQLResponse<Query> response;
            try
            {
                // Execute the query using GraphQL.Client with strongly-typed response
                response = await graphQlClient.SendQueryAsync<Query>(request, cancellationToken);
            }
            catch (GraphQL.Client.Http.GraphQLHttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "GraphQL HTTP request failed for page slug: {PageUrl}. Status: {StatusCode}, Content: {Content}", 
                    pageUrl, httpEx.StatusCode, httpEx.Content);
                throw new InvalidOperationException($"Failed to fetch page from Contentful: {httpEx.Message}", httpEx);
            }
            catch (Newtonsoft.Json.JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize GraphQL response for page slug: {PageUrl}", pageUrl);
                throw new InvalidOperationException($"Invalid response from Contentful GraphQL API: {jsonEx.Message}", jsonEx);
            }

            // Check for GraphQL errors
            if (response.Errors != null && response.Errors.Any())
            {
                var errorMessages = string.Join("; ", response.Errors.Select(e => e.Message));
                _logger.LogError("GraphQL query returned errors: {Errors}", errorMessages);
                throw new InvalidOperationException($"GraphQL query failed: {errorMessages}");
            }

            // Get the strongly-typed result
            var result = response.Data;
            if (result?.PageCollection?.Items == null || !result.PageCollection.Items.Any())
            {
                _logger.LogInformation("No page found with slug: {PageUrl}", pageUrl);
                return null;
            }

            var page = result.PageCollection.Items.FirstOrDefault();
            if (page == null)
            {
                return null;
            }

            // Convert from generated Page model to ContentfulPage application model
            var contentfulPage = MapToContentfulPage(page);

            _logger.LogInformation(
                "Successfully fetched page: {PageUrl} with {SectionCount} sections",
                pageUrl,
                contentfulPage.Sections.Count);

            return contentfulPage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Contentful page with slug: {PageUrl}", pageUrl);
            throw;
        }
    }

    /// <summary>
    /// Maps the generated Page model to the application's ContentfulPage model.
    /// </summary>
    private ContentfulPage MapToContentfulPage(Page page)
    {
        var contentfulPage = new ContentfulPage
        {
            Slug = page.Slug ?? string.Empty,
            Title = page.PageName ?? page.InternalName ?? string.Empty,
            Description = page.InternalName ?? string.Empty,
            Sections = new List<ContentfulSection>(),
            Sys = MapSystemMetadata(page.Sys)
        };

        // Map top sections
        if (page.TopSectionCollection?.Items != null)
        {
            foreach (var sectionItem in page.TopSectionCollection.Items)
            {
                var section = MapSectionItem(sectionItem);
                if (section != null)
                {
                    contentfulPage.Sections.Add(section);
                }
            }
        }

        // Map extra sections
        if (page.ExtraSectionCollection?.Items != null)
        {
            foreach (var sectionItem in page.ExtraSectionCollection.Items)
            {
                var section = MapSectionItem(sectionItem);
                if (section != null)
                {
                    contentfulPage.Sections.Add(section);
                }
            }
        }

        return contentfulPage;
    }

    /// <summary>
    /// Maps a section item (which can be any component type) to ContentfulSection.
    /// </summary>
    private ContentfulSection? MapSectionItem(object? sectionItem)
    {
        if (sectionItem == null)
            return null;

        try
        {
            // Convert the section item to JObject to extract all fields dynamically
            var jObject = JObject.FromObject(sectionItem);

            var section = new ContentfulSection
            {
                Fields = new Dictionary<string, object?>()
            };

            // Extract __typename for content type
            var typename = jObject["__typename"]?.Value<string>();
            if (!string.IsNullOrEmpty(typename))
            {
                section.ContentType = typename;
            }

            // Extract system metadata if available
            if (jObject["sys"] != null)
            {
                var sysObject = jObject["sys"]?.ToObject<Sys>();
                section.Sys = MapSystemMetadata(sysObject);
                if (section.Sys != null)
                {
                    section.Id = section.Sys.Id;
                    // Update content type in Sys if we have it from __typename
                    if (!string.IsNullOrEmpty(typename))
                    {
                        section.Sys.ContentType = typename;
                    }
                }
            }

            // Extract all fields (excluding sys and __typename)
            foreach (var property in jObject.Properties())
            {
                if (property.Name == "sys" || property.Name == "__typename")
                    continue;

                var value = ExtractFieldValue(property.Value);
                if (value != null)
                {
                    section.Fields[property.Name] = value;
                }
            }

            return section;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to map section item");
            return null;
        }
    }

    /// <summary>
    /// Extracts a field value from a JToken, handling nested objects and arrays.
    /// </summary>
    private object? ExtractFieldValue(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                var dict = new Dictionary<string, object>();
                foreach (var prop in ((JObject)token).Properties())
                {
                    var value = ExtractFieldValue(prop.Value);
                    if (value != null)
                    {
                        dict[prop.Name] = value;
                    }
                }
                return dict.Count > 0 ? dict : null;

            case JTokenType.Array:
                var list = new List<object>();
                foreach (var item in (JArray)token)
                {
                    var value = ExtractFieldValue(item);
                    if (value != null)
                    {
                        list.Add(value);
                    }
                }
                return list.Count > 0 ? list : null;

            case JTokenType.String:
                return token.Value<string>();

            case JTokenType.Integer:
                return token.Value<long>();

            case JTokenType.Float:
                return token.Value<double>();

            case JTokenType.Boolean:
                return token.Value<bool>();

            case JTokenType.Date:
                return token.Value<DateTime>();

            case JTokenType.Null:
            case JTokenType.Undefined:
                return null;

            default:
                return token.ToString();
        }
    }

    /// <summary>
    /// Maps Contentful Sys object to ContentfulSystemMetadata.
    /// </summary>
    private ContentfulSystemMetadata? MapSystemMetadata(Sys? sys)
    {
        if (sys == null)
            return null;

        return new ContentfulSystemMetadata
        {
            Id = sys.Id ?? string.Empty,
            ContentType = "unknown", // GraphQL API doesn't provide content type in Sys, use __typename from JSON
            Locale = sys.Locale ?? string.Empty,
            CreatedAt = ConvertToDateTime(sys.FirstPublishedAt),
            UpdatedAt = ConvertToDateTime(sys.PublishedAt),
            Revision = sys.PublishedVersion ?? 0
        };
    }

    /// <summary>
    /// Converts object (which could be DateTime or string) to nullable DateTime.
    /// </summary>
    private DateTime? ConvertToDateTime(object? value)
    {
        if (value == null)
            return null;

        if (value is DateTime dt)
            return dt;

        if (value is string str && DateTime.TryParse(str, out var parsed))
            return parsed;

        return null;
    }
}

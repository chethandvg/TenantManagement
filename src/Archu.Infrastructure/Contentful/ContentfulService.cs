using Archu.Application.Abstractions;
using Archu.Application.Contentful.Models;
using Contentful.Core;
using Contentful.Core.Models;
using Contentful.Core.Search;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Archu.Infrastructure.Contentful;

/// <summary>
/// Implementation of IContentfulService using the Contentful .NET SDK.
/// </summary>
public class ContentfulService : IContentfulService
{
    private readonly IContentfulClient _client;
    private readonly ILogger<ContentfulService> _logger;

    public ContentfulService(IContentfulClient client, ILogger<ContentfulService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ContentfulPage?> GetPageAsync(string pageUrl, string? locale = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            throw new ArgumentException("Page URL cannot be null or empty", nameof(pageUrl));
        }

        _logger.LogInformation("Fetching Contentful page with slug: {PageUrl}, locale: {Locale}", pageUrl, locale ?? "default");

        try
        {
            // Build query to find page by slug
            var queryBuilder = QueryBuilder<Entry<dynamic>>.New
                .ContentTypeIs("page") // Using "page" as the content type ID
                .FieldEquals("fields.slug", pageUrl)
                .Include(3); // Include linked entries up to 3 levels deep

            // Add locale parameter if specified
            if (!string.IsNullOrWhiteSpace(locale))
            {
                queryBuilder = queryBuilder.LocaleIs(locale);
            }

            var entries = await _client.GetEntries(queryBuilder, cancellationToken);

            if (entries == null || !entries.Any())
            {
                _logger.LogInformation("No page found with slug: {PageUrl}", pageUrl);
                return null;
            }

            var entry = entries.FirstOrDefault();
            if (entry == null)
            {
                return null;
            }

            // Parse the entry into our ContentfulPage model
            var page = ParsePage(entry);

            _logger.LogInformation("Successfully fetched page: {PageUrl} with {SectionCount} sections", pageUrl, page.Sections.Count);

            return page;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Contentful page with slug: {PageUrl}", pageUrl);
            throw;
        }
    }

    /// <summary>
    /// Parses a Contentful entry into a ContentfulPage model.
    /// </summary>
    private ContentfulPage ParsePage(Entry<dynamic> entry)
    {
        var page = new ContentfulPage();

        try
        {
            // Access fields from the entry
            var fields = entry.Fields as IDictionary<string, object>;
            if (fields != null)
            {
                if (fields.TryGetValue("slug", out var slug))
                {
                    page.Slug = slug?.ToString() ?? string.Empty;
                }

                if (fields.TryGetValue("title", out var title))
                {
                    page.Title = title?.ToString() ?? string.Empty;
                }

                if (fields.TryGetValue("description", out var description))
                {
                    page.Description = description?.ToString();
                }

                if (fields.TryGetValue("sections", out var sections))
                {
                    page.Sections = ParseSections(sections);
                }
            }

            // Parse system metadata
            if (entry.SystemProperties != null)
            {
                page.Sys = new ContentfulSystemMetadata
                {
                    Id = entry.SystemProperties.Id ?? string.Empty,
                    ContentType = entry.SystemProperties.ContentType?.SystemProperties?.Id ?? string.Empty,
                    Locale = entry.SystemProperties.Locale,
                    CreatedAt = entry.SystemProperties.CreatedAt,
                    UpdatedAt = entry.SystemProperties.UpdatedAt,
                    Revision = entry.SystemProperties.Revision
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Contentful page entry");
            throw;
        }

        return page;
    }

    /// <summary>
    /// Parses sections from a Contentful entry field.
    /// </summary>
    private List<ContentfulSection> ParseSections(object? sectionsObj)
    {
        var sectionsList = new List<ContentfulSection>();

        if (sectionsObj == null)
        {
            return sectionsList;
        }

        try
        {
            // Sections might be an array/list of entries
            if (sectionsObj is IEnumerable<object> sectionsEnumerable)
            {
                foreach (var sectionEntry in sectionsEnumerable)
                {
                    var section = ParseSection(sectionEntry);
                    if (section != null)
                    {
                        sectionsList.Add(section);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing sections");
        }

        return sectionsList;
    }

    /// <summary>
    /// Parses a single section from a Contentful entry.
    /// </summary>
    private ContentfulSection? ParseSection(object? sectionObj)
    {
        if (sectionObj == null)
        {
            return null;
        }

        var section = new ContentfulSection();

        try
        {
            // Try to access the section as an Entry
            if (sectionObj is Entry<dynamic> sectionEntry)
            {
                // Get system properties
                if (sectionEntry.SystemProperties != null)
                {
                    section.Id = sectionEntry.SystemProperties.Id ?? string.Empty;
                    section.ContentType = sectionEntry.SystemProperties.ContentType?.SystemProperties?.Id ?? string.Empty;

                    section.Sys = new ContentfulSystemMetadata
                    {
                        Id = sectionEntry.SystemProperties.Id ?? string.Empty,
                        ContentType = sectionEntry.SystemProperties.ContentType?.SystemProperties?.Id ?? string.Empty,
                        Locale = sectionEntry.SystemProperties.Locale,
                        CreatedAt = sectionEntry.SystemProperties.CreatedAt,
                        UpdatedAt = sectionEntry.SystemProperties.UpdatedAt,
                        Revision = sectionEntry.SystemProperties.Revision
                    };
                }

                // Get fields
                var fields = sectionEntry.Fields as IDictionary<string, object>;
                if (fields != null)
                {
                    // Convert to nullable dictionary
                    section.Fields = fields.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing individual section, skipping");
            return null;
        }

        return section;
    }
}

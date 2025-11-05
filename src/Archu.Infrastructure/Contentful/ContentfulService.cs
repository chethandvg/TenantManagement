using Archu.Application.Abstractions;
using Archu.Application.Contentful.Models;
using Archu.Infrastructure.Contentful.Models;
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
            // Build query to find page by slug using Entry wrapper
            var queryBuilder = QueryBuilder<Entry<ContentfulPageEntry>>.New
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
    private ContentfulPage ParsePage(Entry<ContentfulPageEntry> entry)
    {
        var page = new ContentfulPage();

        try
        {
            // Access fields from the strongly-typed entry
            if (entry.Fields != null)
            {
                page.Slug = entry.Fields.Slug ?? string.Empty;
                page.Title = entry.Fields.Title ?? string.Empty;
                page.Description = entry.Fields.Description;
                
                // Parse sections if available
                if (entry.Fields.Sections != null && entry.Fields.Sections.Any())
                {
                    page.Sections = ParseSections(entry.Fields.Sections);
                }
                else
                {
                    page.Sections = new List<ContentfulSection>();
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
    private List<ContentfulSection> ParseSections(List<object>? sectionsObj)
    {
        var sectionsList = new List<ContentfulSection>();

        if (sectionsObj == null || !sectionsObj.Any())
        {
            return sectionsList;
        }

        try
        {
            sectionsList = sectionsObj
                .Select(sectionEntry => ParseSection(sectionEntry))
                .Where(section => section != null)
                .Cast<ContentfulSection>()
                .ToList();
        }
        catch (InvalidCastException ex)
        {
            _logger.LogError(ex, "Invalid cast while parsing sections");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument while parsing sections");
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
            // Serialize and deserialize to handle dynamic types properly
            var json = JsonSerializer.Serialize(sectionObj);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract system properties
            if (root.TryGetProperty("sys", out var sysElement))
            {
                if (sysElement.TryGetProperty("id", out var idElement))
                {
                    section.Id = idElement.GetString() ?? string.Empty;
                }

                if (sysElement.TryGetProperty("contentType", out var contentTypeElement))
                {
                    if (contentTypeElement.TryGetProperty("sys", out var ctSysElement))
                    {
                        if (ctSysElement.TryGetProperty("id", out var ctIdElement))
                        {
                            section.ContentType = ctIdElement.GetString() ?? string.Empty;
                        }
                    }
                }

                section.Sys = new ContentfulSystemMetadata
                {
                    Id = section.Id,
                    ContentType = section.ContentType,
                    Locale = sysElement.TryGetProperty("locale", out var localeEl) ? localeEl.GetString() : null,
                    CreatedAt = sysElement.TryGetProperty("createdAt", out var createdEl) && createdEl.TryGetDateTime(out var created) ? created : null,
                    UpdatedAt = sysElement.TryGetProperty("updatedAt", out var updatedEl) && updatedEl.TryGetDateTime(out var updated) ? updated : null,
                    Revision = sysElement.TryGetProperty("revision", out var revEl) && revEl.TryGetInt32(out var rev) ? rev : null
                };
            }

            // Extract fields
            if (root.TryGetProperty("fields", out var fieldsElement))
            {
                var fieldsDict = new Dictionary<string, object?>();
                
                foreach (var field in fieldsElement.EnumerateObject())
                {
                    fieldsDict[field.Name] = ExtractJsonValue(field.Value);
                }
                
                section.Fields = fieldsDict;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON error while parsing section, skipping");
            return null;
        }
        catch (InvalidCastException ex)
        {
            _logger.LogWarning(ex, "Invalid cast while parsing section, skipping");
            return null;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null argument while parsing section, skipping");
            return null;
        }

        return section;
    }

    /// <summary>
    /// Extracts a value from a JsonElement.
    /// </summary>
    private object? ExtractJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ExtractJsonValue(p.Value)),
            _ => element.GetRawText()
        };
    }
}

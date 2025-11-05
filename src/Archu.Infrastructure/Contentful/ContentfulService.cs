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
            // Build query to find page by slug
            var queryBuilder = QueryBuilder<ContentfulPageEntry>.New
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
    private ContentfulPage ParsePage(ContentfulPageEntry entry)
    {
        var page = new ContentfulPage();

        try
        {
            // Access fields from the strongly-typed entry
            page.Slug = entry.Slug ?? string.Empty;
            
            // Use PageName as primary title, fallback to Title for backward compatibility
            page.Title = entry.PageName ?? entry.Title ?? string.Empty;
            
            // Use InternalName as description if Description is not available
            page.Description = entry.Description ?? entry.InternalName;
            
            // Parse sections from either TopSection or Sections field
            var sectionsToProcess = entry.TopSection ?? entry.Sections;
            
            if (sectionsToProcess != null && sectionsToProcess.Any())
            {
                page.Sections = ParseSectionsFromJObjects(sectionsToProcess);
            }
            else
            {
                page.Sections = new List<ContentfulSection>();
            }

            // Note: System metadata (Sys) is not available when using QueryBuilder<ContentfulPageEntry>
            // If you need system metadata, use QueryBuilder<Entry<ContentfulPageEntry>> instead
            page.Sys = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Contentful page entry");
            throw;
        }

        return page;
    }

    /// <summary>
    /// Parses sections from JObject list (flexible approach for dynamic Contentful schemas).
    /// </summary>
    private List<ContentfulSection> ParseSectionsFromJObjects(List<Newtonsoft.Json.Linq.JObject>? jobjects)
    {
        var sectionsList = new List<ContentfulSection>();

        if (jobjects == null || !jobjects.Any())
        {
            return sectionsList;
        }

        try
        {
            foreach (var jobj in jobjects)
            {
                var section = ParseSectionFromJObject(jobj);
                if (section != null)
                {
                    sectionsList.Add(section);
                }
            }
        }
        catch (InvalidCastException ex)
        {
            _logger.LogError(ex, "Invalid cast while parsing sections from JObjects");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument while parsing sections from JObjects");
        }

        return sectionsList;
    }

    /// <summary>
    /// Parses a single section from a JObject.
    /// </summary>
    private ContentfulSection? ParseSectionFromJObject(Newtonsoft.Json.Linq.JObject? jobj)
    {
        if (jobj == null)
        {
            return null;
        }

        var section = new ContentfulSection();

        try
        {
            // Extract system properties
            if (jobj["sys"] is Newtonsoft.Json.Linq.JObject sysObj)
            {
                section.Id = sysObj["id"]?.ToString() ?? string.Empty;
                
                if (sysObj["contentType"]?["sys"]?["id"] is Newtonsoft.Json.Linq.JToken ctId)
                {
                    section.ContentType = ctId.ToString();
                }

                section.Sys = new ContentfulSystemMetadata
                {
                    Id = section.Id,
                    ContentType = section.ContentType,
                    Locale = sysObj["locale"]?.ToString(),
                    CreatedAt = sysObj["createdAt"]?.ToObject<DateTime?>(),
                    UpdatedAt = sysObj["updatedAt"]?.ToObject<DateTime?>(),
                    Revision = sysObj["revision"]?.ToObject<int?>()
                };
            }

            // Extract all fields (excluding sys and $metadata)
            var fieldsDict = new Dictionary<string, object?>();
            foreach (var prop in jobj.Properties())
            {
                if (prop.Name != "sys" && prop.Name != "$metadata" && prop.Name != "$id")
                {
                    fieldsDict[prop.Name] = ExtractJTokenValue(prop.Value);
                }
            }
            
            section.Fields = fieldsDict;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON error while parsing section from JObject, skipping");
            return null;
        }
        catch (InvalidCastException ex)
        {
            _logger.LogWarning(ex, "Invalid cast while parsing section from JObject, skipping");
            return null;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null argument while parsing section from JObject, skipping");
            return null;
        }

        return section;
    }

    /// <summary>
    /// Extracts a value from a JToken.
    /// </summary>
    private object? ExtractJTokenValue(Newtonsoft.Json.Linq.JToken token)
    {
        return token.Type switch
        {
            Newtonsoft.Json.Linq.JTokenType.String => token.ToString(),
            Newtonsoft.Json.Linq.JTokenType.Integer => token.ToObject<long>(),
            Newtonsoft.Json.Linq.JTokenType.Float => token.ToObject<double>(),
            Newtonsoft.Json.Linq.JTokenType.Boolean => token.ToObject<bool>(),
            Newtonsoft.Json.Linq.JTokenType.Null => null,
            Newtonsoft.Json.Linq.JTokenType.Array => token.Select(ExtractJTokenValue).ToList(),
            Newtonsoft.Json.Linq.JTokenType.Object => token.ToObject<Dictionary<string, object?>>(),
            _ => token.ToString()
        };
    }
}

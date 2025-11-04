# Contentful Integration

This document describes the Contentful CMS integration in the Archu application.

## Overview

The Contentful integration provides a public API endpoint to retrieve page content from Contentful CMS, including all dynamic sections and components. The implementation follows Clean Architecture principles and uses the CQRS pattern with MediatR.

## Architecture

### Layers

1. **API Layer** (`Archu.Api`)
   - `ContentfulController`: Exposes the REST API endpoint
   - Route: `/api/v1/contentful/{pageUrl}`
   - Public access (no authentication required)

2. **Application Layer** (`Archu.Application`)
   - `IContentfulService`: Service interface for Contentful operations
   - `GetContentfulPageQuery`: MediatR query
   - `GetContentfulPageQueryHandler`: Query handler
   - Models: `ContentfulPage`, `ContentfulSection`, `ContentfulSystemMetadata`

3. **Contracts Layer** (`Archu.Contracts`)
   - `ContentfulPageDto`: API response DTO
   - `ContentfulSectionDto`: Section response DTO

4. **Infrastructure Layer** (`Archu.Infrastructure`)
   - `ContentfulService`: Implementation using Contentful .NET SDK
   - `ContentfulSettings`: Configuration options
   - `NullContentfulService`: Null Object pattern for unconfigured scenarios

## Configuration

### Required Settings

Add the following to your `appsettings.json` or environment variables:

```json
{
  "Contentful": {
    "SpaceId": "your-contentful-space-id",
    "DeliveryApiKey": "your-contentful-delivery-api-key",
    "Environment": "master"
  }
}
```

### Environment Variables (Recommended for Production)

```bash
Contentful__SpaceId=your-space-id
Contentful__DeliveryApiKey=your-api-key
Contentful__Environment=master
```

## API Usage

### Endpoint

```
GET /api/v1/contentful/{pageUrl}?locale={locale}
```

### Parameters

- `pageUrl` (required): The page slug/URL to retrieve
- `locale` (optional): The language/locale code (e.g., "en-US", "de-DE")

### Example Request

```bash
curl https://api.example.com/api/v1/contentful/home?locale=en-US
```

### Example Response

```json
{
  "success": true,
  "data": {
    "slug": "home",
    "title": "Home Page",
    "description": "Welcome to our home page",
    "sections": [
      {
        "id": "abc123",
        "contentType": "heroSection",
        "fields": {
          "heading": "Welcome",
          "subheading": "To our amazing site",
          "image": { /* asset object */ }
        }
      },
      {
        "id": "def456",
        "contentType": "textBlock",
        "fields": {
          "text": "Our story begins..."
        }
      }
    ]
  },
  "message": "Page retrieved successfully",
  "timestamp": "2025-11-04T15:30:00Z"
}
```

### Error Responses

#### Page Not Found (404)

```json
{
  "success": false,
  "message": "Page 'nonexistent' not found",
  "timestamp": "2025-11-04T15:30:00Z"
}
```

#### Server Error (500)

```json
{
  "success": false,
  "message": "An error occurred while retrieving the page",
  "timestamp": "2025-11-04T15:30:00Z"
}
```

## Contentful Content Model

### Page Content Type

The integration expects a "page" content type in Contentful with the following fields:

- `slug` (Short text): Unique page URL identifier
- `title` (Short text): Page title
- `description` (Long text, optional): Page description
- `sections` (References, multiple): Array of linked section entries

### Section Content Types

Sections can be any content type. Common examples:

- `heroSection`: Hero banner with heading, text, and image
- `textBlock`: Rich text content
- `imageGallery`: Collection of images
- `callToAction`: CTA with button and link

The integration automatically includes all fields for each section type.

## Localization

### Supported Locales

The integration supports any locale configured in your Contentful space. Common examples:

- `en-US`: English (United States)
- `de-DE`: German (Germany)
- `fr-FR`: French (France)
- `es-ES`: Spanish (Spain)

### Default Locale

If no locale is specified in the request, Contentful's default locale will be used.

### Locale Fallback

Contentful automatically falls back to the default locale for untranslated fields.

## Implementation Details

### Include Depth

The integration fetches linked entries up to **3 levels deep**. This ensures:
- Page → Sections are resolved
- Section → Nested assets/references are resolved
- Deep nested content is included

### Dynamic Sections

Sections are returned as a flexible structure with:
- `id`: Contentful entry ID
- `contentType`: The content type identifier
- `fields`: Dictionary of all fields for that content type

This allows the frontend to render different section types dynamically.

### Null Object Pattern

If Contentful is not configured (missing SpaceId or DeliveryApiKey), the application will:
1. Start successfully (no exceptions during DI registration)
2. Return `null` for all page requests (behaves as if no pages exist)
3. Allow the application to function without Contentful

## Testing

### Unit Tests

Location: `tests/Archu.UnitTests/Application/Contentful/Queries/GetContentfulPageQueryHandlerTests.cs`

Test coverage includes:
- ✅ Page retrieval (with and without locale)
- ✅ Multiple sections handling
- ✅ Localization support (multiple locales)
- ✅ Error handling (not found, exceptions)
- ✅ Property mapping
- ✅ Edge cases (null descriptions, empty sections)

Run tests:
```bash
dotnet test --filter "Feature=Contentful"
```

### Manual Testing

1. Configure Contentful credentials in `appsettings.Development.json`
2. Run the API: `dotnet run --project src/Archu.Api`
3. Test the endpoint: `curl https://localhost:7001/api/v1/contentful/your-page-slug`

## Security

### Public Access

The Contentful endpoint is marked with `[AllowAnonymous]` because:
- Content pages are typically public-facing
- No sensitive data is exposed
- Follows the same pattern as static content delivery

### API Key Security

- ⚠️ Never commit API keys to source control
- ✅ Use environment variables in production
- ✅ Rotate keys regularly
- ✅ Use Contentful's Delivery API key (read-only)

### Input Validation

- Page URL is validated (cannot be null or whitespace)
- Contentful SDK handles query sanitization
- No SQL injection risk (using Contentful's API)

## Dependencies

- `contentful.csharp` v8.2.0
  - Official Contentful .NET SDK
  - No known security vulnerabilities
  - Maintained by Contentful

## Troubleshooting

### "Contentful is not configured" Error

**Cause**: Missing `SpaceId` or `DeliveryApiKey` in configuration

**Solution**: Add the required configuration to `appsettings.json` or environment variables

### Page Not Found (404)

**Possible causes**:
1. Page doesn't exist in Contentful
2. Page slug doesn't match (case-sensitive)
3. Page is not published
4. Wrong locale specified

**Solution**: Verify the page exists and is published in Contentful

### Empty Sections Array

**Possible causes**:
1. Page has no sections linked
2. Sections are not published
3. Include depth is insufficient (rare)

**Solution**: Check that sections are linked and published in Contentful

## Future Enhancements

Potential improvements:
- [ ] Response caching to reduce API calls
- [ ] Preview API support for draft content
- [ ] Webhook integration for cache invalidation
- [ ] GraphQL support for more efficient queries
- [ ] Type-safe section models for specific content types

## References

- [Contentful .NET SDK Documentation](https://github.com/contentful/contentful.net)
- [Contentful Content Delivery API](https://www.contentful.com/developers/docs/references/content-delivery-api/)
- [Archu API Documentation](../README.md)

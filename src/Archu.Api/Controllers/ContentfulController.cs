using Archu.Application.Contentful.Queries.GetContentfulPage;
using Archu.Contracts.Common;
using Archu.Contracts.Contentful;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.Api.Controllers;

/// <summary>
/// Controller for Contentful CMS content retrieval.
/// Provides public access to Contentful pages and dynamic sections.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[AllowAnonymous] // Public access for content pages
public class ContentfulController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContentfulController> _logger;

    public ContentfulController(IMediator mediator, ILogger<ContentfulController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a Contentful page by its URL slug, including all dynamic sections.
    /// </summary>
    /// <remarks>
    /// This endpoint is publicly accessible (no authentication required).
    /// It returns the full page content from Contentful, including all linked sections/components.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/v1/contentful/home?locale=en-US
    /// 
    /// The locale parameter is optional. If not provided, the default Contentful locale will be used.
    /// </remarks>
    /// <param name="pageUrl">The page URL slug (e.g., "home", "about-us").</param>
    /// <param name="locale">Optional locale/language code (e.g., "en-US", "de-DE").</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A standardized API response containing the page with all its sections.</returns>
    /// <response code="200">Returns the requested page with all sections.</response>
    /// <response code="404">If the page is not found.</response>
    /// <response code="500">If there is an error retrieving the page.</response>
    [HttpGet("{pageUrl}")]
    [ProducesResponseType(typeof(ApiResponse<ContentfulPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ContentfulPageDto>>> GetPage(
        string pageUrl,
        [FromQuery] string? locale = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving Contentful page: {PageUrl}, Locale: {Locale}",
                pageUrl,
                locale ?? "default");

            var query = new GetContentfulPageQuery(pageUrl, locale);
            var page = await _mediator.Send(query, cancellationToken);

            if (page is null)
            {
                _logger.LogWarning("Contentful page not found: {PageUrl}", pageUrl);
                return NotFound(ApiResponse<ContentfulPageDto>.Fail($"Page '{pageUrl}' not found"));
            }

            _logger.LogInformation(
                "Successfully retrieved Contentful page: {PageUrl} with {SectionCount} sections",
                pageUrl,
                page.Sections.Count);

            return Ok(ApiResponse<ContentfulPageDto>.Ok(page, "Page retrieved successfully"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Contentful is not configured"))
        {
            _logger.LogError(ex, "Contentful is not configured");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("Contentful CMS is not configured on this server"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Contentful page: {PageUrl}", pageUrl);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("An error occurred while retrieving the page"));
        }
    }
}

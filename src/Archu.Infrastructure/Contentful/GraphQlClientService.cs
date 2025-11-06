using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archu.Infrastructure.Contentful;

/// <summary>
/// Implementation of <see cref="IGraphQlClientService"/> that creates and configures GraphQL HTTP clients
/// for Contentful CMS integration using the GraphQL.Client library.
/// Supports both production (published) and preview (draft) content.
/// </summary>
public class GraphQlClientService : IGraphQlClientService
{
    private readonly IOptions<ContentfulSettings> _settings;
    private readonly ILogger<GraphQlClientService> _logger;
    private GraphQLHttpClient? _cachedClient;
    private GraphQLHttpClient? _cachedPreviewClient;
    private readonly object _lock = new();

    public GraphQlClientService(
        IOptions<ContentfulSettings> settings,
        ILogger<GraphQlClientService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a configured GraphQL HTTP client for Contentful.
    /// Clients are cached for reuse within the service lifetime.
    /// </summary>
    /// <param name="isPreview">If true, returns a client for preview/draft content; otherwise, returns a client for published content.</param>
    /// <returns>A configured <see cref="GraphQLHttpClient"/> instance.</returns>
    public GraphQLHttpClient GetGraphQLClient(bool isPreview = false)
    {
        lock (_lock)
        {
            // Return cached client if available
            if (!isPreview && _cachedClient != null)
            {
                return _cachedClient;
            }

            if (isPreview && _cachedPreviewClient != null)
            {
                return _cachedPreviewClient;
            }

            var settings = _settings.Value;
            
            // Determine which API key to use
            var apiKey = isPreview && !string.IsNullOrWhiteSpace(settings.PreviewApiKey)
                ? settings.PreviewApiKey
                : settings.DeliveryApiKey;

            // Construct the GraphQL endpoint URL
            var endpoint = settings.GetGraphQlEndpoint();

            _logger.LogInformation(
                "Creating GraphQL client for Contentful - Endpoint: {Endpoint}, IsPreview: {IsPreview}",
                endpoint,
                isPreview);

            // Create a new GraphQL HTTP client using GraphQL.Client library
            var graphQlClient = new GraphQLHttpClient(
                endpoint,
                new NewtonsoftJsonSerializer());

            // Configure authorization header with Bearer token
            graphQlClient.HttpClient.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {apiKey}");

            // Add optional headers for better performance and debugging
            graphQlClient.HttpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            graphQlClient.HttpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");

            // Set timeout for requests (30 seconds)
            graphQlClient.HttpClient.Timeout = TimeSpan.FromSeconds(30);

            // Cache the client for reuse
            if (isPreview)
            {
                _cachedPreviewClient = graphQlClient;
            }
            else
            {
                _cachedClient = graphQlClient;
            }

            return graphQlClient;
        }
    }
}

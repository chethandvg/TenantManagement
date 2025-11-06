using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Archu.Infrastructure.Contentful;

/// <summary>
/// HTTP client for executing GraphQL queries against Contentful's GraphQL API.
/// </summary>
public class GraphQlContentfulClient
{
    private readonly HttpClient _httpClient;
    private readonly ContentfulSettings _settings;
    private readonly ILogger<GraphQlContentfulClient> _logger;

    public GraphQlContentfulClient(
        HttpClient httpClient,
        IOptions<ContentfulSettings> settings,
        ILogger<GraphQlContentfulClient> _logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_settings.GetGraphQlEndpoint());
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _settings.DeliveryApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Executes a GraphQL query and returns the deserialized result.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="query">The GraphQL query string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response data.</returns>
    public async Task<T?> ExecuteQueryAsync<T>(
        string query,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty", nameof(query));

        _logger.LogDebug("Executing GraphQL query: {Query}", query);

        var request = new { query };
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("GraphQL response: {Response}", responseContent);

        var result = JsonConvert.DeserializeObject<GraphQLResponse<T>>(responseContent);

        if (result?.Errors != null && result.Errors.Any())
        {
            var errorMessages = string.Join("; ", result.Errors.Select(e => e.Message));
            _logger.LogError("GraphQL query returned errors: {Errors}", errorMessages);
            throw new InvalidOperationException($"GraphQL query failed: {errorMessages}");
        }

        return result?.Data;
    }

    /// <summary>
    /// GraphQL response wrapper.
    /// </summary>
    private class GraphQLResponse<T>
    {
        [JsonProperty("data")]
        public T? Data { get; set; }

        [JsonProperty("errors")]
        public List<GraphQLError>? Errors { get; set; }
    }

    /// <summary>
    /// GraphQL error model.
    /// </summary>
    private class GraphQLError
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("locations")]
        public List<GraphQLErrorLocation>? Locations { get; set; }

        [JsonProperty("path")]
        public List<string>? Path { get; set; }
    }

    /// <summary>
    /// GraphQL error location.
    /// </summary>
    private class GraphQLErrorLocation
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }
    }
}

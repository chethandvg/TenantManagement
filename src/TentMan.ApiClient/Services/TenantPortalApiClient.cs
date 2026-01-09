using TentMan.Contracts.Common;
using TentMan.Contracts.Tenants;
using TentMan.Contracts.TenantPortal;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Tenant Portal API client.
/// </summary>
public sealed class TenantPortalApiClient : ApiClientServiceBase, ITenantPortalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TenantPortalApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantPortalApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public TenantPortalApiClient(HttpClient httpClient, ILogger<TenantPortalApiClient> logger)
        : base(httpClient, logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/v1/tenant-portal";

    /// <inheritdoc/>
    public Task<ApiResponse<TenantLeaseSummaryResponse>> GetLeaseSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<TenantLeaseSummaryResponse>("lease-summary", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<TenantDocumentDto>>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<TenantDocumentDto>>("documents", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<TenantDocumentDto>> UploadDocumentAsync(
        Stream file,
        string fileName,
        string contentType,
        TenantDocumentUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var uri = $"{BasePath}/documents";
        _logger.LogDebug("Uploading document to {Uri}", uri);

        try
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            // Add request fields
            content.Add(new StringContent(((int)request.DocType).ToString()), "docType");
            if (!string.IsNullOrEmpty(request.DocNumberMasked))
                content.Add(new StringContent(request.DocNumberMasked), "docNumberMasked");
            if (request.IssueDate.HasValue)
                content.Add(new StringContent(request.IssueDate.Value.ToString("yyyy-MM-dd")), "issueDate");
            if (request.ExpiryDate.HasValue)
                content.Add(new StringContent(request.ExpiryDate.Value.ToString("yyyy-MM-dd")), "expiryDate");
            if (!string.IsNullOrEmpty(request.Notes))
                content.Add(new StringContent(request.Notes), "notes");

            var response = await _httpClient.PostAsync(uri, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TenantDocumentDto>>(
                    _jsonOptions,
                    cancellationToken);

                if (apiResponse == null)
                {
                    _logger.LogWarning("Received null response from server for {RequestUri}", uri);
                    return ApiResponse<TenantDocumentDto>.Fail("Received null response from server");
                }

                return apiResponse;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Document upload failed with status {StatusCode}: {Error}",
                (int)response.StatusCode, errorContent);

            return ApiResponse<TenantDocumentDto>.Fail($"Upload failed: {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document to {Uri}", uri);
            return ApiResponse<TenantDocumentDto>.Fail($"Upload error: {ex.Message}");
        }
    }
}

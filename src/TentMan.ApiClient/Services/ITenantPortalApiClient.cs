using TentMan.Contracts.Common;
using TentMan.Contracts.Tenants;
using TentMan.Contracts.TenantPortal;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Tenant Portal API client.
/// </summary>
public interface ITenantPortalApiClient
{
    /// <summary>
    /// Gets the current tenant's active lease summary.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant's lease summary.</returns>
    Task<ApiResponse<TenantLeaseSummaryResponse>> GetLeaseSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of tenant documents.</returns>
    Task<ApiResponse<IEnumerable<TenantDocumentDto>>> GetDocumentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a document for the current tenant.
    /// </summary>
    /// <param name="file">The file stream to upload.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="request">The document upload request details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The uploaded document details.</returns>
    Task<ApiResponse<TenantDocumentDto>> UploadDocumentAsync(
        Stream file,
        string fileName,
        string contentType,
        TenantDocumentUploadRequest request,
        CancellationToken cancellationToken = default);
}

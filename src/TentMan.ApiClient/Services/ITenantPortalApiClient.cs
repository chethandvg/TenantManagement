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

    /// <summary>
    /// Gets the move-in handover checklist for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The move-in handover checklist.</returns>
    Task<ApiResponse<MoveInHandoverResponse>> GetMoveInHandoverAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits the move-in handover checklist with tenant signature.
    /// </summary>
    /// <param name="request">The handover submission request.</param>
    /// <param name="signatureImage">The signature image stream.</param>
    /// <param name="signatureFileName">The signature file name.</param>
    /// <param name="signatureContentType">The signature content type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated handover details.</returns>
    Task<ApiResponse<MoveInHandoverResponse>> SubmitMoveInHandoverAsync(
        SubmitHandoverRequest request,
        Stream signatureImage,
        string signatureFileName,
        string signatureContentType,
        CancellationToken cancellationToken = default);
}

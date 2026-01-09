using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Tenants;

/// <summary>
/// Request to upload a tenant document via the portal.
/// </summary>
public class TenantDocumentUploadRequest
{
    public DocumentType DocType { get; set; }
    public string? DocNumberMasked { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

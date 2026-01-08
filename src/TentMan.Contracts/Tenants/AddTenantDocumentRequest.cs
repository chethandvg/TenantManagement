using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Tenants;

/// <summary>
/// Request to add a document for a tenant.
/// </summary>
public class AddTenantDocumentRequest
{
    public Guid? LeaseId { get; set; }
    public DocumentType DocType { get; set; }
    public string? DocNumberMasked { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public Guid FileId { get; set; }
    public string? Notes { get; set; }
}

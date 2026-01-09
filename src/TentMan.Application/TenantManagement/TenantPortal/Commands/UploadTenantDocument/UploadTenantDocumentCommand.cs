using TentMan.Contracts.Tenants;
using MediatR;

namespace TentMan.Application.TenantManagement.TenantPortal.Commands.UploadTenantDocument;

/// <summary>
/// Command to upload a tenant document.
/// </summary>
public record UploadTenantDocumentCommand(
    Guid TenantId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    TenantDocumentUploadRequest Request) : IRequest<TenantDocumentDto>;

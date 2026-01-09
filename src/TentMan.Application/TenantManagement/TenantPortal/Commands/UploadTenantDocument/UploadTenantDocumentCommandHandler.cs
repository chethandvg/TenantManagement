using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Storage;
using TentMan.Application.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Tenants;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace TentMan.Application.TenantManagement.TenantPortal.Commands.UploadTenantDocument;

public class UploadTenantDocumentCommandHandler : BaseCommandHandler, IRequestHandler<UploadTenantDocumentCommand, TenantDocumentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedContentTypes = {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public UploadTenantDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUser currentUser,
        ILogger<UploadTenantDocumentCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<TenantDocumentDto> Handle(UploadTenantDocumentCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Uploading document for tenant {TenantId}", request.TenantId);

        // Validate file size
        if (request.SizeBytes > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB");
        }

        // Validate content type
        if (!AllowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException($"File type '{request.ContentType}' is not allowed. Allowed types: PDF, JPEG, PNG, DOC, DOCX");
        }

        // Verify tenant exists
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {request.TenantId} not found");
        }

        // Verify user has access to this tenant (if user context available)
        if (CurrentUser.UserId != null)
        {
            var tenantByUser = await _unitOfWork.Tenants.GetByLinkedUserIdAsync(Guid.Parse(CurrentUser.UserId), cancellationToken);
            if (tenantByUser == null || tenantByUser.Id != request.TenantId)
            {
                throw new UnauthorizedAccessException("You do not have access to upload documents for this tenant");
            }
        }

        // Store file and create metadata
        var fileMetadata = await StoreFileAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            tenant.OrgId,
            cancellationToken);

        // Create tenant document record
        var document = new TenantDocument
        {
            TenantId = request.TenantId,
            DocType = request.Request.DocType,
            DocNumberMasked = request.Request.DocNumberMasked,
            IssueDate = request.Request.IssueDate,
            ExpiryDate = request.Request.ExpiryDate,
            FileId = fileMetadata.Id,
            Notes = request.Request.Notes,
            CreatedByUserId = CurrentUser.UserId != null ? Guid.Parse(CurrentUser.UserId) : null,
            CreatedBy = CurrentUser.UserId ?? "System"
        };

        await _unitOfWork.TenantDocuments.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Document {DocumentId} uploaded for tenant {TenantId}", document.Id, request.TenantId);

        return new TenantDocumentDto
        {
            Id = document.Id,
            DocType = document.DocType,
            DocNumberMasked = document.DocNumberMasked,
            IssueDate = document.IssueDate,
            ExpiryDate = document.ExpiryDate,
            FileId = document.FileId,
            FileName = fileMetadata.FileName,
            Notes = document.Notes
        };
    }

    private async Task<FileMetadata> StoreFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid orgId,
        CancellationToken cancellationToken)
    {
        // Calculate SHA256 hash
        string sha256Hash;
        using (var sha256 = SHA256.Create())
        {
            fileStream.Position = 0;
            var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
            sha256Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // Upload file to Azure Blob Storage
        fileStream.Position = 0;
        var storageKey = await _fileStorageService.UploadFileAsync(
            fileStream,
            fileName,
            contentType,
            "tenant-documents",
            cancellationToken);

        // Create file metadata
        var fileMetadata = new FileMetadata
        {
            OrgId = orgId,
            StorageProvider = StorageProvider.AzureBlob,
            StorageKey = storageKey,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Sha256 = sha256Hash,
            CreatedByUserId = CurrentUser.UserId != null ? Guid.Parse(CurrentUser.UserId) : null,
            CreatedBy = CurrentUser.UserId ?? "System"
        };

        await _unitOfWork.FileMetadata.AddAsync(fileMetadata, cancellationToken);

        return fileMetadata;
    }
}

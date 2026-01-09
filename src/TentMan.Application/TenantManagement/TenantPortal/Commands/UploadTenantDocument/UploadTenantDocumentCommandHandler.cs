using TentMan.Application.Abstractions;
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
        ICurrentUser currentUser,
        ILogger<UploadTenantDocumentCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
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
        // Create storage directory if it doesn't exist
        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tenant-documents");
        Directory.CreateDirectory(storageRoot);

        // Generate unique file name
        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        var storageFileName = $"{fileId}{extension}";
        var storagePath = Path.Combine(storageRoot, storageFileName);

        // Calculate SHA256 hash
        string sha256Hash;
        using (var sha256 = SHA256.Create())
        {
            fileStream.Position = 0;
            var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
            sha256Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // Save file to disk
        fileStream.Position = 0;
        using (var fileOutput = File.Create(storagePath))
        {
            await fileStream.CopyToAsync(fileOutput, cancellationToken);
        }

        // Create file metadata
        var fileMetadata = new FileMetadata
        {
            OrgId = orgId,
            StorageProvider = StorageProvider.Local,
            StorageKey = Path.Combine("uploads", "tenant-documents", storageFileName),
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

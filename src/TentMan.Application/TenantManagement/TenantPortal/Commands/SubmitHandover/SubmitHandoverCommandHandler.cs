using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Storage;
using TentMan.Application.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.TenantPortal;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace TentMan.Application.TenantManagement.TenantPortal.Commands.SubmitHandover;

public class SubmitHandoverCommandHandler : BaseCommandHandler, IRequestHandler<SubmitHandoverCommand, MoveInHandoverResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private const long MaxSignatureSizeBytes = 2 * 1024 * 1024; // 2MB
    private static readonly string[] AllowedSignatureTypes = {
        "image/png",
        "image/jpeg",
        "image/jpg"
    };

    public SubmitHandoverCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUser currentUser,
        ILogger<SubmitHandoverCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    public async Task<MoveInHandoverResponse> Handle(SubmitHandoverCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Submitting handover {HandoverId} for user {UserId}", request.Request.HandoverId, request.UserId);

        // Get handover with details
        var handover = await _unitOfWork.UnitHandovers.GetByIdWithDetailsAsync(request.Request.HandoverId, cancellationToken);
        if (handover == null)
        {
            throw new InvalidOperationException($"Handover {request.Request.HandoverId} not found");
        }

        // Verify user has access
        var tenant = await _unitOfWork.Tenants.GetByLinkedUserIdAsync(request.UserId, cancellationToken);
        if (tenant == null)
        {
            throw new UnauthorizedAccessException("Tenant not found for current user");
        }

        // Verify tenant is part of the lease
        var isPartOfLease = handover.Lease.Parties.Any(p => p.TenantId == tenant.Id && !p.IsDeleted);
        if (!isPartOfLease)
        {
            throw new UnauthorizedAccessException("You do not have access to this handover");
        }

        // Check if already signed
        if (handover.SignedByTenant)
        {
            throw new InvalidOperationException("Handover has already been signed by tenant");
        }

        // Validate signature image
        if (request.SignatureImageBytes == null || request.SignatureImageBytes.Length == 0)
        {
            throw new InvalidOperationException("Signature image is required");
        }

        if (request.SignatureImageBytes.Length > MaxSignatureSizeBytes)
        {
            throw new InvalidOperationException($"Signature image exceeds maximum size of {MaxSignatureSizeBytes / 1024 / 1024}MB");
        }

        if (!AllowedSignatureTypes.Contains((request.SignatureContentType ?? "").ToLowerInvariant()))
        {
            throw new InvalidOperationException($"Signature image type '{request.SignatureContentType}' is not allowed. Allowed types: PNG, JPEG");
        }

        // Update checklist items
        foreach (var itemUpdate in request.Request.ChecklistItems)
        {
            var item = handover.ChecklistItems.FirstOrDefault(ci => ci.Id == itemUpdate.Id);
            if (item != null)
            {
                item.Condition = itemUpdate.Condition;
                item.Remarks = itemUpdate.Remarks;
                item.ModifiedAtUtc = DateTime.UtcNow;
                item.ModifiedBy = request.UserId.ToString();
            }
        }

        // Update meter readings
        foreach (var meterUpdate in request.Request.MeterReadings)
        {
            var meter = handover.Lease.MeterReadings.FirstOrDefault(mr => mr.Id == meterUpdate.MeterId);
            if (meter != null)
            {
                meter.ReadingValue = meterUpdate.Reading;
                meter.ReadingDate = meterUpdate.ReadingDate;
                meter.ModifiedAtUtc = DateTime.UtcNow;
                meter.ModifiedBy = request.UserId.ToString();
            }
        }

        // Update handover (prepare for DB save)
        var originalRowVersion = handover.RowVersion;
        handover.SignedByTenant = true;
        handover.Notes = request.Request.Notes;
        handover.ModifiedAtUtc = DateTime.UtcNow;
        handover.ModifiedBy = request.UserId.ToString();

        // Save to database first to get FileMetadata ID
        var fileMetadata = new FileMetadata
        {
            OrgId = handover.Lease.OrgId,
            StorageProvider = StorageProvider.AzureBlob,
            StorageKey = string.Empty, // Will be updated after upload
            FileName = request.SignatureFileName,
            ContentType = request.SignatureContentType,
            SizeBytes = request.SignatureImageBytes.Length,
            Sha256 = string.Empty, // Will be calculated during upload
            CreatedByUserId = CurrentUser.UserId != null ? Guid.Parse(CurrentUser.UserId) : null,
            CreatedBy = CurrentUser.UserId ?? "System"
        };

        await _unitOfWork.FileMetadata.AddAsync(fileMetadata, cancellationToken);
        handover.SignatureTenantFileId = fileMetadata.Id;

        await _unitOfWork.UnitHandovers.UpdateAsync(handover, originalRowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Now upload the file to blob storage
        try
        {
            using var signatureStream = new MemoryStream(request.SignatureImageBytes);
            var (storageKey, sha256Hash) = await StoreSignatureAsync(
                signatureStream,
                request.SignatureFileName,
                request.SignatureContentType,
                cancellationToken);

            // Update file metadata with storage details
            fileMetadata.StorageKey = storageKey;
            fileMetadata.Sha256 = sha256Hash;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // If file upload fails, we should ideally rollback the database changes
            // For now, log the error and rethrow
            Logger.LogError("Failed to upload signature file for handover {HandoverId}", handover.Id);
            throw;
        }

        Logger.LogInformation("Handover {HandoverId} signed by tenant {TenantId}", handover.Id, tenant.Id);

        // Return updated handover
        return new MoveInHandoverResponse
        {
            HandoverId = handover.Id,
            LeaseId = handover.LeaseId,
            UnitNumber = handover.Lease.Unit?.UnitNumber ?? "",
            BuildingName = handover.Lease.Unit?.Building?.Name ?? "",
            Date = handover.Date,
            IsCompleted = handover.SignedByTenant,
            Notes = handover.Notes,
            ChecklistItems = handover.ChecklistItems
                .Where(ci => !ci.IsDeleted)
                .Select(ci => new HandoverChecklistItemDto
                {
                    Id = ci.Id,
                    Category = ci.Category,
                    ItemName = ci.ItemName,
                    Condition = ci.Condition,
                    Remarks = ci.Remarks,
                    PhotoFileId = ci.PhotoFileId,
                    PhotoFileName = ci.PhotoFile?.FileName
                }).ToList(),
            MeterReadings = handover.Lease.MeterReadings
                .Where(mr => !mr.IsDeleted)
                .Select(mr => new MeterReadingDto
                {
                    MeterId = mr.Id,
                    MeterType = mr.MeterType.ToString(),
                    Reading = mr.ReadingValue,
                    ReadingDate = mr.ReadingDate
                }).ToList()
        };
    }

    private async Task<(string storageKey, string sha256Hash)> StoreSignatureAsync(
        Stream signatureStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        // Calculate SHA256 hash
        string sha256Hash;
        using (var sha256 = SHA256.Create())
        {
            signatureStream.Position = 0;
            var hashBytes = await sha256.ComputeHashAsync(signatureStream, cancellationToken);
            sha256Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // Upload signature to Azure Blob Storage
        signatureStream.Position = 0;
        var storageKey = await _fileStorageService.UploadFileAsync(
            signatureStream,
            fileName,
            contentType,
            "handover-signatures",
            cancellationToken);

        return (storageKey, sha256Hash);
    }
}

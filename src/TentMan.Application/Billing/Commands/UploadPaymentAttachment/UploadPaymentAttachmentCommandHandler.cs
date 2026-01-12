using MediatR;
using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Abstractions.Storage;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Commands.UploadPaymentAttachment;

/// <summary>
/// Handler for uploading payment attachments (receipts, screenshots, etc.).
/// Uploads file to Azure Blob and creates PaymentAttachment record.
/// </summary>
public class UploadPaymentAttachmentCommandHandler : IRequestHandler<UploadPaymentAttachmentCommand, UploadPaymentAttachmentResult>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UploadPaymentAttachmentCommandHandler> _logger;

    public UploadPaymentAttachmentCommandHandler(
        IPaymentRepository paymentRepository,
        IFileStorageService fileStorageService,
        IFileMetadataRepository fileMetadataRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<UploadPaymentAttachmentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _fileStorageService = fileStorageService;
        _fileMetadataRepository = fileMetadataRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UploadPaymentAttachmentResult> Handle(UploadPaymentAttachmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate payment exists
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                return new UploadPaymentAttachmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment with ID {request.PaymentId} not found"
                };
            }

            // Validate file
            if (request.FileStream == null || string.IsNullOrWhiteSpace(request.FileName))
            {
                return new UploadPaymentAttachmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "File is required"
                };
            }

            if (request.FileSize <= 0 || request.FileSize > 10 * 1024 * 1024) // 10 MB limit
            {
                return new UploadPaymentAttachmentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "File size must be between 1 byte and 10 MB"
                };
            }

            // Upload file to Azure Blob
            var storageKey = await _fileStorageService.UploadFileAsync(
                request.FileStream,
                request.FileName,
                request.ContentType ?? "application/octet-stream",
                "payment-receipts",
                cancellationToken);

            // Create file metadata record
            var fileMetadata = new FileMetadata
            {
                Id = Guid.NewGuid(),
                OrgId = payment.OrgId,
                StorageProvider = Contracts.Enums.StorageProvider.AzureBlob,
                StorageKey = storageKey,
                FileName = request.FileName,
                ContentType = request.ContentType ?? "application/octet-stream",
                SizeBytes = request.FileSize,
                CreatedByUserId = Guid.TryParse(_currentUser.UserId, out var userId) ? userId : null,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _fileMetadataRepository.AddAsync(fileMetadata, cancellationToken);

            // Calculate display order - Note: This has a potential race condition in concurrent uploads
            // In production, consider using a database sequence or handling duplicates at the DB level
            var maxDisplayOrder = 0;
            if (payment.Attachments.Any())
            {
                maxDisplayOrder = payment.Attachments.Max(a => a.DisplayOrder);
            }

            var paymentAttachment = new PaymentAttachment
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                FileId = fileMetadata.Id,
                AttachmentType = request.AttachmentType ?? "Receipt",
                Description = request.Description,
                DisplayOrder = maxDisplayOrder + 1,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _fileMetadataRepository.SavePaymentAttachmentAsync(paymentAttachment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Generate signed URL for immediate access
            var signedUrl = await _fileStorageService.GenerateSignedUrlAsync(storageKey, 60, cancellationToken);

            return new UploadPaymentAttachmentResult
            {
                IsSuccess = true,
                AttachmentId = paymentAttachment.Id,
                FileUrl = signedUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload attachment for payment {PaymentId}", request.PaymentId);
            return new UploadPaymentAttachmentResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to upload attachment."
            };
        }
    }
}

using MediatR;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Abstractions.Storage;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Commands.PaymentConfirmation;

/// <summary>
/// Command to create a payment confirmation request (tenant-initiated).
/// </summary>
public class CreatePaymentConfirmationRequestCommand : IRequest<CreatePaymentConfirmationRequestResult>
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public Stream? ProofFileStream { get; set; }
    public string? ProofFileName { get; set; }
    public string? ProofFileContentType { get; set; }
    public long ProofFileSize { get; set; }
}

/// <summary>
/// Result of creating a payment confirmation request.
/// </summary>
public class CreatePaymentConfirmationRequestResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? RequestId { get; set; }
}

/// <summary>
/// Handler for creating payment confirmation requests.
/// </summary>
public class CreatePaymentConfirmationRequestCommandHandler : IRequestHandler<CreatePaymentConfirmationRequestCommand, CreatePaymentConfirmationRequestResult>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentConfirmationRequestRepository _requestRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileMetadataRepository _fileMetadataRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePaymentConfirmationRequestCommandHandler(
        IInvoiceRepository invoiceRepository,
        IPaymentConfirmationRequestRepository requestRepository,
        IFileStorageService fileStorageService,
        IFileMetadataRepository fileMetadataRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _invoiceRepository = invoiceRepository;
        _requestRepository = requestRepository;
        _fileStorageService = fileStorageService;
        _fileMetadataRepository = fileMetadataRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<CreatePaymentConfirmationRequestResult> Handle(CreatePaymentConfirmationRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate invoice exists
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(request.InvoiceId, cancellationToken);
            if (invoice == null)
            {
                return new CreatePaymentConfirmationRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice with ID {request.InvoiceId} not found"
                };
            }

            // Validate invoice can accept payment confirmations
            if (invoice.Status == InvoiceStatus.Draft || invoice.Status == InvoiceStatus.Voided || invoice.Status == InvoiceStatus.Cancelled)
            {
                return new CreatePaymentConfirmationRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot create payment confirmation for invoice with status: {invoice.Status}"
                };
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                return new CreatePaymentConfirmationRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment amount must be greater than zero"
                };
            }

            if (request.Amount > invoice.BalanceAmount)
            {
                return new CreatePaymentConfirmationRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment amount ({request.Amount:C}) exceeds invoice balance ({invoice.BalanceAmount:C})"
                };
            }

            // Upload proof file if provided
            Guid? proofFileId = null;
            if (request.ProofFileStream != null && !string.IsNullOrEmpty(request.ProofFileName))
            {
                var storageKey = await _fileStorageService.UploadFileAsync(
                    request.ProofFileStream,
                    request.ProofFileName,
                    request.ProofFileContentType ?? "application/octet-stream",
                    "payment-proofs",
                    cancellationToken);

                var fileMetadata = new FileMetadata
                {
                    Id = Guid.NewGuid(),
                    OrgId = invoice.OrgId,
                    StorageProvider = StorageProvider.AzureBlob,
                    StorageKey = storageKey,
                    FileName = request.ProofFileName,
                    ContentType = request.ProofFileContentType ?? "application/octet-stream",
                    SizeBytes = request.ProofFileSize,
                    CreatedByUserId = _currentUser.UserId != null ? Guid.Parse(_currentUser.UserId) : null,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId ?? "System"
                };

                await _fileMetadataRepository.AddAsync(fileMetadata, cancellationToken);
                proofFileId = fileMetadata.Id;
            }

            // Create payment confirmation request
            var confirmationRequest = new PaymentConfirmationRequest
            {
                Id = Guid.NewGuid(),
                OrgId = invoice.OrgId,
                InvoiceId = invoice.Id,
                LeaseId = invoice.LeaseId,
                Amount = request.Amount,
                PaymentDateUtc = request.PaymentDate.ToUniversalTime(),
                ReceiptNumber = request.ReceiptNumber,
                Notes = request.Notes,
                ProofFileId = proofFileId,
                Status = PaymentConfirmationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _requestRepository.AddAsync(confirmationRequest, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreatePaymentConfirmationRequestResult
            {
                IsSuccess = true,
                RequestId = confirmationRequest.Id
            };
        }
        catch (Exception ex)
        {
            return new CreatePaymentConfirmationRequestResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to create payment confirmation request: {ex.Message}"
            };
        }
    }
}

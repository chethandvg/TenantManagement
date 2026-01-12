using MediatR;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Application.Abstractions.Storage;
using TentMan.Contracts.Payments;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Billing.Queries.GetPaymentConfirmationRequests;

/// <summary>
/// Query to get pending payment confirmation requests for an organization.
/// </summary>
public class GetPendingPaymentConfirmationRequestsQuery : IRequest<GetPendingPaymentConfirmationRequestsResult>
{
    public Guid OrgId { get; set; }
}

/// <summary>
/// Result of getting pending payment confirmation requests.
/// </summary>
public class GetPendingPaymentConfirmationRequestsResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<PaymentConfirmationRequestDto> Requests { get; set; } = new List<PaymentConfirmationRequestDto>();
}

/// <summary>
/// Handler for getting pending payment confirmation requests.
/// </summary>
public class GetPendingPaymentConfirmationRequestsQueryHandler : IRequestHandler<GetPendingPaymentConfirmationRequestsQuery, GetPendingPaymentConfirmationRequestsResult>
{
    private readonly IPaymentConfirmationRequestRepository _requestRepository;
    private readonly IFileStorageService _fileStorageService;

    public GetPendingPaymentConfirmationRequestsQueryHandler(
        IPaymentConfirmationRequestRepository requestRepository,
        IFileStorageService fileStorageService)
    {
        _requestRepository = requestRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<GetPendingPaymentConfirmationRequestsResult> Handle(GetPendingPaymentConfirmationRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _requestRepository.GetPendingByOrgIdAsync(request.OrgId, cancellationToken);

            var dtos = new List<PaymentConfirmationRequestDto>();
            foreach (var req in requests)
            {
                string? proofFileUrl = null;
                if (req.ProofFileId.HasValue && req.ProofFile != null)
                {
                    proofFileUrl = await _fileStorageService.GenerateSignedUrlAsync(
                        req.ProofFile.StorageKey,
                        expiresInMinutes: 60,
                        cancellationToken);
                }

                dtos.Add(new PaymentConfirmationRequestDto
                {
                    Id = req.Id,
                    OrgId = req.OrgId,
                    InvoiceId = req.InvoiceId,
                    LeaseId = req.LeaseId,
                    Amount = req.Amount,
                    PaymentDateUtc = req.PaymentDateUtc,
                    ReceiptNumber = req.ReceiptNumber,
                    Notes = req.Notes,
                    ProofFileId = req.ProofFileId,
                    ProofFileUrl = proofFileUrl,
                    ProofFileName = req.ProofFile?.FileName,
                    Status = req.Status,
                    ReviewedAtUtc = req.ReviewedAtUtc,
                    ReviewedBy = req.ReviewedBy,
                    ReviewResponse = req.ReviewResponse,
                    PaymentId = req.PaymentId,
                    CreatedAtUtc = req.CreatedAtUtc,
                    CreatedBy = req.CreatedBy,
                    RowVersion = req.RowVersion
                });
            }

            return new GetPendingPaymentConfirmationRequestsResult
            {
                IsSuccess = true,
                Requests = dtos
            };
        }
        catch (Exception ex)
        {
            return new GetPendingPaymentConfirmationRequestsResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to retrieve payment confirmation requests: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Query to get payment confirmation requests for an invoice.
/// </summary>
public class GetInvoicePaymentConfirmationRequestsQuery : IRequest<GetInvoicePaymentConfirmationRequestsResult>
{
    public Guid InvoiceId { get; set; }
}

/// <summary>
/// Result of getting invoice payment confirmation requests.
/// </summary>
public class GetInvoicePaymentConfirmationRequestsResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<PaymentConfirmationRequestDto> Requests { get; set; } = new List<PaymentConfirmationRequestDto>();
}

/// <summary>
/// Handler for getting invoice payment confirmation requests.
/// </summary>
public class GetInvoicePaymentConfirmationRequestsQueryHandler : IRequestHandler<GetInvoicePaymentConfirmationRequestsQuery, GetInvoicePaymentConfirmationRequestsResult>
{
    private readonly IPaymentConfirmationRequestRepository _requestRepository;
    private readonly IFileStorageService _fileStorageService;

    public GetInvoicePaymentConfirmationRequestsQueryHandler(
        IPaymentConfirmationRequestRepository requestRepository,
        IFileStorageService fileStorageService)
    {
        _requestRepository = requestRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<GetInvoicePaymentConfirmationRequestsResult> Handle(GetInvoicePaymentConfirmationRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var requests = await _requestRepository.GetByInvoiceIdAsync(request.InvoiceId, cancellationToken);

            var dtos = new List<PaymentConfirmationRequestDto>();
            foreach (var req in requests)
            {
                string? proofFileUrl = null;
                if (req.ProofFileId.HasValue && req.ProofFile != null)
                {
                    proofFileUrl = await _fileStorageService.GenerateSignedUrlAsync(
                        req.ProofFile.StorageKey,
                        expiresInMinutes: 60,
                        cancellationToken);
                }

                dtos.Add(new PaymentConfirmationRequestDto
                {
                    Id = req.Id,
                    OrgId = req.OrgId,
                    InvoiceId = req.InvoiceId,
                    LeaseId = req.LeaseId,
                    Amount = req.Amount,
                    PaymentDateUtc = req.PaymentDateUtc,
                    ReceiptNumber = req.ReceiptNumber,
                    Notes = req.Notes,
                    ProofFileId = req.ProofFileId,
                    ProofFileUrl = proofFileUrl,
                    ProofFileName = req.ProofFile?.FileName,
                    Status = req.Status,
                    ReviewedAtUtc = req.ReviewedAtUtc,
                    ReviewedBy = req.ReviewedBy,
                    ReviewResponse = req.ReviewResponse,
                    PaymentId = req.PaymentId,
                    CreatedAtUtc = req.CreatedAtUtc,
                    CreatedBy = req.CreatedBy,
                    RowVersion = req.RowVersion
                });
            }

            return new GetInvoicePaymentConfirmationRequestsResult
            {
                IsSuccess = true,
                Requests = dtos
            };
        }
        catch (Exception ex)
        {
            return new GetInvoicePaymentConfirmationRequestsResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to retrieve payment confirmation requests: {ex.Message}"
            };
        }
    }
}

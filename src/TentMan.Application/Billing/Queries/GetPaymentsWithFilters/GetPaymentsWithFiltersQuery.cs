using MediatR;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Payments;

namespace TentMan.Application.Billing.Queries.GetPaymentsWithFilters;

/// <summary>
/// Query to get payments with advanced filtering.
/// </summary>
public class GetPaymentsWithFiltersQuery : IRequest<GetPaymentsWithFiltersResult>
{
    public Guid OrgId { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? InvoiceId { get; set; }
    public PaymentStatus? Status { get; set; }
    public PaymentMode? PaymentMode { get; set; }
    public PaymentType? PaymentType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? PayerName { get; set; }
    public string? ReceivedBy { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Handler for getting payments with filters.
/// </summary>
public class GetPaymentsWithFiltersQueryHandler : IRequestHandler<GetPaymentsWithFiltersQuery, GetPaymentsWithFiltersResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsWithFiltersQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<GetPaymentsWithFiltersResult> Handle(GetPaymentsWithFiltersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (payments, totalCount) = await _paymentRepository.GetWithFiltersAsync(
                request.OrgId,
                request.LeaseId,
                request.InvoiceId,
                request.Status,
                request.PaymentMode,
                request.PaymentType,
                request.FromDate,
                request.ToDate,
                request.PayerName,
                request.ReceivedBy,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var paymentDtos = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                OrgId = p.OrgId,
                InvoiceId = p.InvoiceId,
                LeaseId = p.LeaseId,
                PaymentMode = p.PaymentMode,
                Status = p.Status,
                Amount = p.Amount,
                PaymentDateUtc = p.PaymentDateUtc,
                TransactionReference = p.TransactionReference,
                ReceivedBy = p.ReceivedBy,
                PayerName = p.PayerName,
                Notes = p.Notes,
                CreatedAtUtc = p.CreatedAtUtc,
                CreatedBy = p.CreatedBy,
                RowVersion = p.RowVersion
            }).ToList();

            return new GetPaymentsWithFiltersResult
            {
                IsSuccess = true,
                Payments = paymentDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
        catch (Exception ex)
        {
            return new GetPaymentsWithFiltersResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to retrieve payments: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Result of getting payments with filters.
/// </summary>
public class GetPaymentsWithFiltersResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

using MediatR;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Payments;

namespace TentMan.Application.Billing.Queries.GetPayments;

/// <summary>
/// Query to get all payments for a specific lease.
/// </summary>
public class GetLeasePaymentsQuery : IRequest<GetLeasePaymentsResult>
{
    public Guid LeaseId { get; set; }
}

/// <summary>
/// Handler for getting lease payments.
/// </summary>
public class GetLeasePaymentsQueryHandler : IRequestHandler<GetLeasePaymentsQuery, GetLeasePaymentsResult>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetLeasePaymentsQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<GetLeasePaymentsResult> Handle(GetLeasePaymentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _paymentRepository.GetByLeaseIdAsync(request.LeaseId, cancellationToken);

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

            return new GetLeasePaymentsResult
            {
                IsSuccess = true,
                Payments = paymentDtos
            };
        }
        catch (Exception ex)
        {
            return new GetLeasePaymentsResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to retrieve lease payments: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Result of getting lease payments.
/// </summary>
public class GetLeasePaymentsResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
}

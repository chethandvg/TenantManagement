using MediatR;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Payments;

namespace TentMan.Application.Billing.Queries.GetPayments;

/// <summary>
/// Handler to get all payments for a specific invoice.
/// </summary>
public class GetInvoicePaymentsQueryHandler : IRequestHandler<GetInvoicePaymentsQuery, GetInvoicePaymentsResult>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoicePaymentsQueryHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<GetInvoicePaymentsResult> Handle(GetInvoicePaymentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify invoice exists
            var invoiceExists = await _invoiceRepository.ExistsAsync(request.InvoiceId, cancellationToken);
            if (!invoiceExists)
            {
                return new GetInvoicePaymentsResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice with ID {request.InvoiceId} not found"
                };
            }

            // Get all payments for the invoice
            var payments = await _paymentRepository.GetByInvoiceIdAsync(request.InvoiceId, cancellationToken);

            // Map to DTOs
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

            return new GetInvoicePaymentsResult
            {
                IsSuccess = true,
                Payments = paymentDtos
            };
        }
        catch (Exception ex)
        {
            return new GetInvoicePaymentsResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to retrieve payments: {ex.Message}"
            };
        }
    }
}

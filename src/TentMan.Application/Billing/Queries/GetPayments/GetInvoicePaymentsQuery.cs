using MediatR;
using TentMan.Contracts.Payments;

namespace TentMan.Application.Billing.Queries.GetPayments;

/// <summary>
/// Query to get all payments for a specific invoice.
/// </summary>
public class GetInvoicePaymentsQuery : IRequest<GetInvoicePaymentsResult>
{
    public Guid InvoiceId { get; set; }
}

/// <summary>
/// Result containing list of payments for an invoice.
/// </summary>
public class GetInvoicePaymentsResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
}

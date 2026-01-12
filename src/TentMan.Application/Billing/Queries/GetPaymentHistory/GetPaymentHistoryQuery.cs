using MediatR;
using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Payments;

namespace TentMan.Application.Billing.Queries.GetPaymentHistory;

/// <summary>
/// Query to get payment status history for a specific payment.
/// </summary>
public class GetPaymentHistoryQuery : IRequest<GetPaymentHistoryResult>
{
    public Guid PaymentId { get; set; }
}

/// <summary>
/// Handler for getting payment status history.
/// </summary>
public class GetPaymentHistoryQueryHandler : IRequestHandler<GetPaymentHistoryQuery, GetPaymentHistoryResult>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<GetPaymentHistoryQueryHandler> _logger;

    public GetPaymentHistoryQueryHandler(
        IPaymentRepository paymentRepository,
        ILogger<GetPaymentHistoryQueryHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<GetPaymentHistoryResult> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if payment exists
            var paymentExists = await _paymentRepository.ExistsAsync(request.PaymentId, cancellationToken);
            if (!paymentExists)
            {
                return new GetPaymentHistoryResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment with ID {request.PaymentId} not found"
                };
            }

            // Get payment history
            var history = await _paymentRepository.GetStatusHistoryAsync(request.PaymentId, cancellationToken);

            var historyDtos = history.Select(h => new PaymentStatusHistoryDto
            {
                Id = h.Id,
                PaymentId = h.PaymentId,
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                ChangedAtUtc = h.ChangedAtUtc,
                ChangedBy = h.ChangedBy,
                Reason = h.Reason,
                Metadata = h.Metadata
            }).ToList();

            return new GetPaymentHistoryResult
            {
                IsSuccess = true,
                History = historyDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve payment history for payment {PaymentId}", request.PaymentId);
            return new GetPaymentHistoryResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to retrieve payment history."
            };
        }
    }
}

/// <summary>
/// Result of getting payment history.
/// </summary>
public class GetPaymentHistoryResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<PaymentStatusHistoryDto> History { get; set; } = new List<PaymentStatusHistoryDto>();
}

using MediatR;
using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Commands.ConfirmRejectPayment;

/// <summary>
/// Command to reject a pending payment.
/// Updates payment status to Rejected and adds status history.
/// </summary>
public class RejectPaymentDirectCommand : IRequest<RejectPaymentResult>
{
    public Guid PaymentId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Handler for rejecting pending payments.
/// </summary>
public class RejectPaymentDirectCommandHandler : IRequestHandler<RejectPaymentDirectCommand, RejectPaymentResult>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RejectPaymentDirectCommandHandler> _logger;

    public RejectPaymentDirectCommandHandler(
        IPaymentRepository paymentRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<RejectPaymentDirectCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<RejectPaymentResult> Handle(RejectPaymentDirectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get payment
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                return new RejectPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment with ID {request.PaymentId} not found"
                };
            }

            // Check if payment can be rejected
            if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.PendingConfirmation)
            {
                return new RejectPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot reject payment with status: {payment.Status}"
                };
            }

            // Validate reason
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return new RejectPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Rejection reason is required"
                };
            }

            // Create status history record
            var statusHistory = new PaymentStatusHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                FromStatus = payment.Status,
                ToStatus = PaymentStatus.Rejected,
                ChangedAtUtc = DateTime.UtcNow,
                ChangedBy = _currentUser.UserId ?? "System",
                Reason = request.Reason,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _paymentRepository.AddStatusHistoryAsync(statusHistory, cancellationToken);

            // Update payment status
            payment.Status = PaymentStatus.Rejected;
            payment.ModifiedAtUtc = DateTime.UtcNow;
            payment.ModifiedBy = _currentUser.UserId ?? "System";

            await _paymentRepository.UpdateAsync(payment, payment.RowVersion, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RejectPaymentResult
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                NewStatus = PaymentStatus.Rejected
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject payment {PaymentId}", request.PaymentId);
            return new RejectPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to reject payment."
            };
        }
    }
}

/// <summary>
/// Result of rejecting a payment.
/// </summary>
public class RejectPaymentResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? PaymentId { get; set; }
    public PaymentStatus? NewStatus { get; set; }
}

using MediatR;
using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Commands.ConfirmRejectPayment;

/// <summary>
/// Command to confirm a pending payment.
/// Updates payment status to Completed and updates invoice accordingly.
/// </summary>
public class ConfirmPaymentCommand : IRequest<ConfirmPaymentResult>
{
    public Guid PaymentId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Handler for confirming pending payments.
/// </summary>
public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, ConfirmPaymentResult>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ConfirmPaymentCommandHandler> _logger;

    public ConfirmPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<ConfirmPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ConfirmPaymentResult> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get payment
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
            if (payment == null)
            {
                return new ConfirmPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment with ID {request.PaymentId} not found"
                };
            }

            // Check if payment can be confirmed
            if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.PendingConfirmation)
            {
                return new ConfirmPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot confirm payment with status: {payment.Status}"
                };
            }

            // Get invoice
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(payment.InvoiceId, cancellationToken);
            if (invoice == null)
            {
                return new ConfirmPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice with ID {payment.InvoiceId} not found"
                };
            }

            // Create status history record
            var statusHistory = new PaymentStatusHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                FromStatus = payment.Status,
                ToStatus = PaymentStatus.Completed,
                ChangedAtUtc = DateTime.UtcNow,
                ChangedBy = _currentUser.UserId ?? "System",
                Reason = request.Notes ?? "Payment confirmed by owner",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _paymentRepository.AddStatusHistoryAsync(statusHistory, cancellationToken);

            // Update payment status
            payment.Status = PaymentStatus.Completed;
            payment.ModifiedAtUtc = DateTime.UtcNow;
            payment.ModifiedBy = _currentUser.UserId ?? "System";

            await _paymentRepository.UpdateAsync(payment, payment.RowVersion, cancellationToken);

            // Persist payment changes before calculating invoice totals
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Update invoice - now includes the confirmed payment
            var totalPaid = await _paymentRepository.GetTotalPaidAmountAsync(invoice.Id, cancellationToken);
            invoice.PaidAmount = totalPaid;
            invoice.BalanceAmount = invoice.TotalAmount - totalPaid;

            if (invoice.BalanceAmount == 0)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAtUtc = DateTime.UtcNow;
            }
            else if (invoice.BalanceAmount < invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            invoice.ModifiedAtUtc = DateTime.UtcNow;
            invoice.ModifiedBy = _currentUser.UserId ?? "System";

            await _invoiceRepository.UpdateAsync(invoice, invoice.RowVersion, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ConfirmPaymentResult
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                NewStatus = PaymentStatus.Completed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payment {PaymentId}", request.PaymentId);
            return new ConfirmPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to confirm payment."
            };
        }
    }
}

/// <summary>
/// Result of confirming a payment.
/// </summary>
public class ConfirmPaymentResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? PaymentId { get; set; }
    public PaymentStatus? NewStatus { get; set; }
}

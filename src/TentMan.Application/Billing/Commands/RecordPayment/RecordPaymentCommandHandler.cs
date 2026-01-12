using MediatR;
using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Commands.RecordPayment;

/// <summary>
/// Unified handler for recording payments (cash, online, etc.) via /api/payments endpoint.
/// Consolidates payment recording logic for different payment modes.
/// </summary>
public class RecordPaymentCommandHandler : IRequestHandler<RecordUnifiedPaymentCommand, RecordPaymentResult>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RecordPaymentCommandHandler> _logger;

    public RecordPaymentCommandHandler(
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<RecordPaymentCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<RecordPaymentResult> Handle(RecordUnifiedPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate invoice exists and get details
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(request.InvoiceId, cancellationToken);
            if (invoice == null)
            {
                return new RecordPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice with ID {request.InvoiceId} not found"
                };
            }

            // Validate invoice can accept payments
            if (invoice.Status == InvoiceStatus.Draft || invoice.Status == InvoiceStatus.Voided || invoice.Status == InvoiceStatus.Cancelled)
            {
                return new RecordPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot record payment for invoice with status: {invoice.Status}"
                };
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                return new RecordPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment amount must be greater than zero"
                };
            }

            // Validate transaction reference for non-cash payments
            if (request.PaymentMode != PaymentMode.Cash && string.IsNullOrWhiteSpace(request.TransactionReference))
            {
                return new RecordPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Transaction reference is required for non-cash payments"
                };
            }

            // Get current total paid
            var totalPaid = await _paymentRepository.GetTotalPaidAmountAsync(request.InvoiceId, cancellationToken);
            var remainingBalance = invoice.TotalAmount - totalPaid;

            if (request.Amount > remainingBalance)
            {
                return new RecordPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment amount ({request.Amount:C}) exceeds remaining balance ({remainingBalance:C})"
                };
            }

            // Determine initial payment status based on payment mode
            // All online payments should start as Pending for proper review workflow
            var paymentStatus = request.PaymentMode switch
            {
                PaymentMode.Cash => PaymentStatus.Completed,  // Cash payments are immediately completed
                PaymentMode.Online => PaymentStatus.Pending,  // All online payments start pending for review
                _ => PaymentStatus.Completed  // Other modes (UPI, BankTransfer, Cheque with ref) are marked completed
            };

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrgId = invoice.OrgId,
                InvoiceId = invoice.Id,
                LeaseId = invoice.LeaseId,
                PaymentType = request.PaymentType,
                PaymentMode = request.PaymentMode,
                Status = paymentStatus,
                Amount = request.Amount,
                PaymentDateUtc = request.PaymentDate.ToUniversalTime(),
                TransactionReference = request.TransactionReference,
                GatewayTransactionId = request.GatewayTransactionId,
                GatewayName = request.GatewayName,
                ReceivedBy = _currentUser.UserId ?? "System",
                PayerName = request.PayerName,
                Notes = request.Notes,
                PaymentMetadata = request.PaymentMetadata,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);

            // Update invoice paid amount and status (only if payment is completed)
            if (paymentStatus == PaymentStatus.Completed)
            {
                var newTotalPaid = totalPaid + request.Amount;
                invoice.PaidAmount = newTotalPaid;
                invoice.BalanceAmount = invoice.TotalAmount - newTotalPaid;

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
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RecordPaymentResult
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                InvoiceBalanceAmount = invoice.BalanceAmount
            };
        }
        catch (Exception ex)
        {
            // Check if this is a concurrency exception by checking the exception type name
            // We can't reference EntityFrameworkCore in the Application layer
            if (ex.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(ex, "Concurrency conflict when recording payment for invoice {InvoiceId}", request.InvoiceId);
                return new RecordPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invoice was modified by another process. Please retry."
                };
            }

            _logger.LogError(ex, "Failed to record payment for invoice {InvoiceId}", request.InvoiceId);
            return new RecordPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to record payment."
            };
        }
    }
}

/// <summary>
/// Command to record a payment via unified /api/payments endpoint.
/// Supports all payment modes (Cash, Online, UPI, BankTransfer, Cheque, etc.)
/// </summary>
public class RecordUnifiedPaymentCommand : IRequest<RecordPaymentResult>
{
    public Guid InvoiceId { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Rent;
    public PaymentMode PaymentMode { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? TransactionReference { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? GatewayName { get; set; }
    public string? PayerName { get; set; }
    public string? Notes { get; set; }
    public string? PaymentMetadata { get; set; }
}

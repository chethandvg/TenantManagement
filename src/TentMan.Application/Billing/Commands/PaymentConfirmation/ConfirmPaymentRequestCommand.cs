using MediatR;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Commands.PaymentConfirmation;

/// <summary>
/// Command to confirm a payment confirmation request (owner-initiated).
/// Creates a payment record and updates invoice status.
/// </summary>
public class ConfirmPaymentRequestCommand : IRequest<ConfirmPaymentRequestResult>
{
    public Guid RequestId { get; set; }
    public string? ReviewResponse { get; set; }
}

/// <summary>
/// Result of confirming a payment request.
/// </summary>
public class ConfirmPaymentRequestResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? PaymentId { get; set; }
}

/// <summary>
/// Handler for confirming payment confirmation requests.
/// </summary>
public class ConfirmPaymentRequestCommandHandler : IRequestHandler<ConfirmPaymentRequestCommand, ConfirmPaymentRequestResult>
{
    private readonly IPaymentConfirmationRequestRepository _requestRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ConfirmPaymentRequestCommandHandler(
        IPaymentConfirmationRequestRepository requestRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _requestRepository = requestRepository;
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<ConfirmPaymentRequestResult> Handle(ConfirmPaymentRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the confirmation request
            var confirmationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (confirmationRequest == null)
            {
                return new ConfirmPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment confirmation request with ID {request.RequestId} not found"
                };
            }

            // Validate request is in pending status
            if (confirmationRequest.Status != PaymentConfirmationStatus.Pending)
            {
                return new ConfirmPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment confirmation request is not pending. Current status: {confirmationRequest.Status}"
                };
            }

            // Get the invoice
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(confirmationRequest.InvoiceId, cancellationToken);
            if (invoice == null)
            {
                return new ConfirmPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Associated invoice not found"
                };
            }

            // Validate invoice can accept payments
            if (invoice.Status == InvoiceStatus.Draft || invoice.Status == InvoiceStatus.Voided || invoice.Status == InvoiceStatus.Cancelled)
            {
                return new ConfirmPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot confirm payment for invoice with status: {invoice.Status}"
                };
            }

            // Get current total paid to validate
            var totalPaid = await _paymentRepository.GetTotalPaidAmountAsync(invoice.Id, cancellationToken);
            var remainingBalance = invoice.TotalAmount - totalPaid;

            if (confirmationRequest.Amount > remainingBalance)
            {
                return new ConfirmPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment amount ({confirmationRequest.Amount:C}) exceeds remaining balance ({remainingBalance:C})"
                };
            }

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrgId = invoice.OrgId,
                InvoiceId = invoice.Id,
                LeaseId = invoice.LeaseId,
                PaymentMode = PaymentMode.Cash,
                Status = PaymentStatus.Completed,
                Amount = confirmationRequest.Amount,
                PaymentDateUtc = confirmationRequest.PaymentDateUtc,
                TransactionReference = confirmationRequest.ReceiptNumber,
                ReceivedBy = _currentUser.UserId ?? "System",
                PayerName = confirmationRequest.CreatedBy,
                Notes = $"Confirmed from payment request. {confirmationRequest.Notes}",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);

            // Update invoice paid amount and status
            var newTotalPaid = totalPaid + confirmationRequest.Amount;
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

            // Update confirmation request status
            confirmationRequest.Status = PaymentConfirmationStatus.Confirmed;
            confirmationRequest.ReviewedAtUtc = DateTime.UtcNow;
            confirmationRequest.ReviewedBy = _currentUser.UserId ?? "System";
            confirmationRequest.ReviewResponse = request.ReviewResponse;
            confirmationRequest.PaymentId = payment.Id;
            confirmationRequest.ModifiedAtUtc = DateTime.UtcNow;
            confirmationRequest.ModifiedBy = _currentUser.UserId ?? "System";

            await _requestRepository.UpdateAsync(confirmationRequest, confirmationRequest.RowVersion, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ConfirmPaymentRequestResult
            {
                IsSuccess = true,
                PaymentId = payment.Id
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return new ConfirmPaymentRequestResult
            {
                IsSuccess = false,
                ErrorMessage = "Request was modified by another process. Please retry."
            };
        }
        catch (Exception ex)
        {
            return new ConfirmPaymentRequestResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to confirm payment request: {ex.Message}"
            };
        }
    }
}

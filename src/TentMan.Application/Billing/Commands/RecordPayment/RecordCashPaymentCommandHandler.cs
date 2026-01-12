using MediatR;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Commands.RecordPayment;

/// <summary>
/// Handler for recording cash payments.
/// Cash payments are immediately marked as completed.
/// </summary>
public class RecordCashPaymentCommandHandler : IRequestHandler<RecordCashPaymentCommand, RecordPaymentResult>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RecordCashPaymentCommandHandler(
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<RecordPaymentResult> Handle(RecordCashPaymentCommand request, CancellationToken cancellationToken)
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

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrgId = invoice.OrgId,
                InvoiceId = invoice.Id,
                LeaseId = invoice.LeaseId,
                PaymentMode = PaymentMode.Cash,
                Status = PaymentStatus.Completed, // Cash payments are immediately completed
                Amount = request.Amount,
                PaymentDateUtc = request.PaymentDate.ToUniversalTime(),
                TransactionReference = request.ReceiptNumber,
                ReceivedBy = _currentUser.UserId ?? "System",
                PayerName = request.PayerName,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);

            // Update invoice paid amount and status
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
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RecordPaymentResult
            {
                IsSuccess = true,
                PaymentId = payment.Id,
                InvoiceBalanceAmount = invoice.BalanceAmount
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return new RecordPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Invoice was modified by another process. Please retry."
            };
        }
        catch (Exception ex)
        {
            return new RecordPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to record payment: {ex.Message}"
            };
        }
    }
}

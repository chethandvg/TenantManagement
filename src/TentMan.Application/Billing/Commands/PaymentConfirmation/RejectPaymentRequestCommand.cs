using MediatR;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Billing.Commands.PaymentConfirmation;

/// <summary>
/// Command to reject a payment confirmation request (owner-initiated).
/// </summary>
public class RejectPaymentRequestCommand : IRequest<RejectPaymentRequestResult>
{
    public Guid RequestId { get; set; }
    public string ReviewResponse { get; set; } = string.Empty;
}

/// <summary>
/// Result of rejecting a payment request.
/// </summary>
public class RejectPaymentRequestResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Handler for rejecting payment confirmation requests.
/// </summary>
public class RejectPaymentRequestCommandHandler : IRequestHandler<RejectPaymentRequestCommand, RejectPaymentRequestResult>
{
    private readonly IPaymentConfirmationRequestRepository _requestRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RejectPaymentRequestCommandHandler(
        IPaymentConfirmationRequestRepository requestRepository,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _requestRepository = requestRepository;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<RejectPaymentRequestResult> Handle(RejectPaymentRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the confirmation request
            var confirmationRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (confirmationRequest == null)
            {
                return new RejectPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment confirmation request with ID {request.RequestId} not found"
                };
            }

            // Validate request is in pending status
            if (confirmationRequest.Status != PaymentConfirmationStatus.Pending)
            {
                return new RejectPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Payment confirmation request is not pending. Current status: {confirmationRequest.Status}"
                };
            }

            // Validate review response is provided
            if (string.IsNullOrWhiteSpace(request.ReviewResponse))
            {
                return new RejectPaymentRequestResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Review response is required when rejecting a payment request"
                };
            }

            // Update confirmation request status
            confirmationRequest.Status = PaymentConfirmationStatus.Rejected;
            confirmationRequest.ReviewedAtUtc = DateTime.UtcNow;
            confirmationRequest.ReviewedBy = _currentUser.UserId ?? "System";
            confirmationRequest.ReviewResponse = request.ReviewResponse;
            confirmationRequest.ModifiedAtUtc = DateTime.UtcNow;
            confirmationRequest.ModifiedBy = _currentUser.UserId ?? "System";

            await _requestRepository.UpdateAsync(confirmationRequest, confirmationRequest.RowVersion, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RejectPaymentRequestResult
            {
                IsSuccess = true
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return new RejectPaymentRequestResult
            {
                IsSuccess = false,
                ErrorMessage = "Request was modified by another process. Please retry."
            };
        }
        catch (Exception ex)
        {
            return new RejectPaymentRequestResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to reject payment request: {ex.Message}"
            };
        }
    }
}

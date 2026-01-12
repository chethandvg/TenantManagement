using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for PaymentConfirmationRequest entity operations.
/// </summary>
public interface IPaymentConfirmationRequestRepository
{
    /// <summary>
    /// Gets a payment confirmation request by ID.
    /// </summary>
    Task<PaymentConfirmationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payment confirmation requests for a specific invoice.
    /// </summary>
    Task<IEnumerable<PaymentConfirmationRequest>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payment confirmation requests for a specific lease.
    /// </summary>
    Task<IEnumerable<PaymentConfirmationRequest>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending payment confirmation requests for an organization.
    /// </summary>
    Task<IEnumerable<PaymentConfirmationRequest>> GetPendingByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payment confirmation requests for an organization with a specific status.
    /// </summary>
    Task<IEnumerable<PaymentConfirmationRequest>> GetByOrgIdAndStatusAsync(Guid orgId, PaymentConfirmationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new payment confirmation request.
    /// </summary>
    Task<PaymentConfirmationRequest> AddAsync(PaymentConfirmationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment confirmation request.
    /// </summary>
    Task UpdateAsync(PaymentConfirmationRequest request, byte[] rowVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a payment confirmation request.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment confirmation request exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

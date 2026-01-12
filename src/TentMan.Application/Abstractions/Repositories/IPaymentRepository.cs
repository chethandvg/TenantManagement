using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for Payment entity operations.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Gets a payment by ID.
    /// </summary>
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payments for a specific invoice.
    /// </summary>
    Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payments for a specific lease.
    /// </summary>
    Task<IEnumerable<Payment>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payments for an organization.
    /// </summary>
    Task<IEnumerable<Payment>> GetByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new payment.
    /// </summary>
    Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment.
    /// </summary>
    Task UpdateAsync(Payment payment, byte[] rowVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a payment.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total paid amount for an invoice.
    /// </summary>
    Task<decimal> GetTotalPaidAmountAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}

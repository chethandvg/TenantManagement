using Microsoft.EntityFrameworkCore;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Payment entity.
/// </summary>
public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Invoice)
            .Include(p => p.Lease)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.InvoiceId == invoiceId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.LeaseId == leaseId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.OrgId == orgId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(payment, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Payment payment, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(payment, rowVersion);
        DbSet.Update(payment);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await DbSet
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        
        if (payment != null)
        {
            SoftDelete(payment);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<decimal> GetTotalPaidAmountAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.InvoiceId == invoiceId 
                     && !p.IsDeleted 
                     && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, cancellationToken);
    }

    public async Task<(IEnumerable<Payment> Payments, int TotalCount)> GetWithFiltersAsync(
        Guid orgId,
        Guid? leaseId = null,
        Guid? invoiceId = null,
        PaymentStatus? status = null,
        PaymentMode? paymentMode = null,
        PaymentType? paymentType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? payerName = null,
        string? receivedBy = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => p.OrgId == orgId && !p.IsDeleted);

        if (leaseId.HasValue)
            query = query.Where(p => p.LeaseId == leaseId.Value);

        if (invoiceId.HasValue)
            query = query.Where(p => p.InvoiceId == invoiceId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (paymentMode.HasValue)
            query = query.Where(p => p.PaymentMode == paymentMode.Value);

        if (paymentType.HasValue)
            query = query.Where(p => p.PaymentType == paymentType.Value);

        if (fromDate.HasValue)
            query = query.Where(p => p.PaymentDateUtc >= fromDate.Value.ToUniversalTime());

        if (toDate.HasValue)
            query = query.Where(p => p.PaymentDateUtc <= toDate.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(payerName))
            query = query.Where(p => p.PayerName != null && p.PayerName.Contains(payerName));

        if (!string.IsNullOrWhiteSpace(receivedBy))
            query = query.Where(p => p.ReceivedBy.Contains(receivedBy));

        var totalCount = await query.CountAsync(cancellationToken);

        var payments = await query
            .OrderByDescending(p => p.PaymentDateUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (payments, totalCount);
    }

    public async Task<IEnumerable<PaymentStatusHistory>> GetStatusHistoryAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentStatusHistory>()
            .Where(h => h.PaymentId == paymentId && !h.IsDeleted)
            .OrderByDescending(h => h.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddStatusHistoryAsync(PaymentStatusHistory history, CancellationToken cancellationToken = default)
    {
        await Context.Set<PaymentStatusHistory>().AddAsync(history, cancellationToken);
    }
}

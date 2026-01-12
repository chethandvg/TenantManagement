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
}

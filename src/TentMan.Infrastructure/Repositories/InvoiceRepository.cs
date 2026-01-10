using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class InvoiceRepository : BaseRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.ChargeType)
            .Include(i => i.Lease)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.LeaseId == leaseId)
            .Include(i => i.Lines.Where(l => !l.IsDeleted))
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> GetByOrgIdAsync(Guid orgId, InvoiceStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(i => i.OrgId == orgId);

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        return await query
            .Include(i => i.Lease)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetDraftInvoiceForPeriodAsync(
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(i =>
                i.LeaseId == leaseId &&
                i.Status == InvoiceStatus.Draft &&
                i.BillingPeriodStart == billingPeriodStart &&
                i.BillingPeriodEnd == billingPeriodEnd,
                cancellationToken);
    }

    public async Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(invoice, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Invoice invoice, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(invoice, originalRowVersion);
        DbSet.Update(invoice);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);
    }
}

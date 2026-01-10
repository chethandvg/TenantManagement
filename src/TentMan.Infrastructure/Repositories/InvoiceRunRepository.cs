using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class InvoiceRunRepository : BaseRepository<InvoiceRun>, IInvoiceRunRepository
{
    public InvoiceRunRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<InvoiceRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<InvoiceRun?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Lease)
            .Include(r => r.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Invoice)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<InvoiceRun>> GetByOrgIdAsync(Guid orgId, InvoiceRunStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(r => r.OrgId == orgId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceRun> AddAsync(InvoiceRun invoiceRun, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(invoiceRun, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(InvoiceRun invoiceRun, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(invoiceRun, originalRowVersion);
        DbSet.Update(invoiceRun);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }
}

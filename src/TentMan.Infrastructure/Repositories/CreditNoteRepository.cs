using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class CreditNoteRepository : BaseRepository<CreditNote>, ICreditNoteRepository
{
    public CreditNoteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);
    }

    public async Task<CreditNote?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(cn => cn.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.InvoiceLine)
            .Include(cn => cn.Invoice)
            .FirstOrDefaultAsync(cn => cn.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<CreditNote>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(cn => cn.InvoiceId == invoiceId)
            .Include(cn => cn.Lines.Where(l => !l.IsDeleted))
            .OrderByDescending(cn => cn.CreditNoteDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CreditNote>> GetByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(cn => cn.OrgId == orgId)
            .Include(cn => cn.Invoice)
            .OrderByDescending(cn => cn.CreditNoteDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<CreditNote> AddAsync(CreditNote creditNote, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(creditNote, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(CreditNote creditNote, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(creditNote, originalRowVersion);
        DbSet.Update(creditNote);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(cn => cn.Id == id && !cn.IsDeleted, cancellationToken);
    }
}

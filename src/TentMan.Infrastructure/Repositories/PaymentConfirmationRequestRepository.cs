using Microsoft.EntityFrameworkCore;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for PaymentConfirmationRequest entity.
/// </summary>
public class PaymentConfirmationRequestRepository : BaseRepository<PaymentConfirmationRequest>, IPaymentConfirmationRequestRepository
{
    public PaymentConfirmationRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaymentConfirmationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Invoice)
            .Include(p => p.Lease)
            .Include(p => p.ProofFile)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<PaymentConfirmationRequest>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.ProofFile)
            .Where(p => p.InvoiceId == invoiceId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentConfirmationRequest>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Invoice)
            .Include(p => p.ProofFile)
            .Where(p => p.LeaseId == leaseId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentConfirmationRequest>> GetPendingByOrgIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Invoice)
            .Include(p => p.Lease)
            .Include(p => p.ProofFile)
            .Where(p => p.OrgId == orgId && p.Status == PaymentConfirmationStatus.Pending && !p.IsDeleted)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentConfirmationRequest>> GetByOrgIdAndStatusAsync(Guid orgId, PaymentConfirmationStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Invoice)
            .Include(p => p.Lease)
            .Include(p => p.ProofFile)
            .Where(p => p.OrgId == orgId && p.Status == status && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentConfirmationRequest> AddAsync(PaymentConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(request, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(PaymentConfirmationRequest request, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        SetOriginalRowVersion(request, rowVersion);
        DbSet.Update(request);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await DbSet
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        
        if (request != null)
        {
            SoftDelete(request);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }
}

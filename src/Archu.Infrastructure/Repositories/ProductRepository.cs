using Archu.Application.Abstractions;
using Archu.Domain.Entities;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Archu.Infrastructure.Repositories;

/// <summary>
/// Implements product data access operations using Entity Framework Core.
/// Note: This repository only tracks changes. Call IUnitOfWork.SaveChangesAsync() to persist.
/// </summary>
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetByIdAndOwnerAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, cancellationToken);
    }

    public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        DbSet.Add(product);
        return Task.FromResult(product);
    }

    public Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        // ✅ Set the original RowVersion to enable concurrency detection
        SetOriginalRowVersion(product, originalRowVersion);

        DbSet.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        // ✅ Soft delete is handled by BaseRepository and ApplicationDbContext
        SoftDelete(product);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAndIsOwnedByAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .AnyAsync(p => p.Id == id && p.OwnerId == ownerId, cancellationToken);
    }

    public async Task<bool> IsOwnedByAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .AnyAsync(p => p.Id == resourceId && p.OwnerId == userId, cancellationToken);
    }
}

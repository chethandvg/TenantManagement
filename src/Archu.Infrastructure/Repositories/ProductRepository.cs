using Archu.Application.Abstractions;
using Archu.Domain.Entities;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Archu.Infrastructure.Repositories;

/// <summary>
/// Implements product data access operations using Entity Framework Core.
/// Note: This repository only tracks changes. Call IUnitOfWork.SaveChangesAsync() to persist.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        return Task.FromResult(product);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Remove(product);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == id, cancellationToken);
    }
}

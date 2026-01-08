using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class OwnerRepository : IOwnerRepository
{
    private readonly ApplicationDbContext _context;

    public OwnerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Owner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Owners
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Owner>> GetByOrganizationIdAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        return await _context.Owners
            .Where(o => o.OrgId == orgId)
            .OrderBy(o => o.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Owner> AddAsync(Owner owner, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Owners.AddAsync(owner, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Owner owner, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        _context.Entry(owner).OriginalValues[nameof(owner.RowVersion)] = originalRowVersion;
        _context.Owners.Update(owner);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Owners.AnyAsync(o => o.Id == id, cancellationToken);
    }
}

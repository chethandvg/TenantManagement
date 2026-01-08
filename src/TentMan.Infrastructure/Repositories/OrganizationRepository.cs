using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly ApplicationDbContext _context;

    public OrganizationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Organization> AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Organizations.AddAsync(organization, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Organization organization, byte[] originalRowVersion, CancellationToken cancellationToken = default)
    {
        _context.Entry(organization).OriginalValues[nameof(organization.RowVersion)] = originalRowVersion;
        _context.Organizations.Update(organization);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Organizations.AnyAsync(o => o.Id == id, cancellationToken);
    }
}

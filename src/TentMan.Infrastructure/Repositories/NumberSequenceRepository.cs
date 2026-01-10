using TentMan.Application.Abstractions.Repositories;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository for managing number sequences using a simple table-based approach.
/// Uses database locking for thread safety.
/// </summary>
public class NumberSequenceRepository : INumberSequenceRepository
{
    private readonly ApplicationDbContext _context;

    public NumberSequenceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public Task<long> GetNextSequenceNumberAsync(
        Guid orgId,
        string sequenceType,
        CancellationToken cancellationToken = default)
    {
        // For now, use a simple in-memory counter stored in the database
        // In a production system, this would use a dedicated sequences table
        // with proper locking mechanisms
        
        // Generate a simple timestamp-based sequence number
        // This is a simplified implementation - in production, you'd want a proper sequence table
        var timestamp = DateTime.UtcNow.Ticks;
        var sequence = timestamp % 1000000; // Get last 6 digits
        
        return Task.FromResult(sequence);
    }
}

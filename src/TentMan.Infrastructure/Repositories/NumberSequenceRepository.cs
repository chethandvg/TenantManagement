using TentMan.Application.Abstractions.Repositories;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository for managing number sequences using a simple table-based approach.
/// NOTE: This is a simplified implementation for development/testing.
/// In production, this should use a dedicated sequences table with proper row-level locking
/// or database-native sequence features to ensure thread-safety and true sequential numbers.
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
        // IMPORTANT: This is a simplified implementation for development/testing purposes only.
        // It is NOT thread-safe and may produce duplicate sequence numbers under concurrent load.
        // 
        // For production use, implement one of the following:
        // 1. Use a dedicated NumberSequences table with row-level locking (e.g., SQL Server UPDLOCK, ROWLOCK)
        // 2. Use database-native sequences (e.g., SQL Server SEQUENCE objects)
        // 3. Use a distributed lock mechanism (e.g., Redis)
        
        // Generate a simple timestamp-based sequence number
        var timestamp = DateTime.UtcNow.Ticks;
        var sequence = timestamp % 1000000; // Get last 6 digits
        
        return Task.FromResult(sequence);
    }
}

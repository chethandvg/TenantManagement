using TentMan.Application.Abstractions.Repositories;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class FileMetadataRepository : BaseRepository<FileMetadata>, IFileMetadataRepository
{
    public FileMetadataRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<FileMetadata> AddAsync(FileMetadata file, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(file, cancellationToken);
        return entry.Entity;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
    }

    public async Task<PaymentAttachment> SavePaymentAttachmentAsync(PaymentAttachment attachment, CancellationToken cancellationToken = default)
    {
        var entry = await Context.Set<PaymentAttachment>().AddAsync(attachment, cancellationToken);
        return entry.Entity;
    }
}

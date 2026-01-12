using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions.Repositories;

/// <summary>
/// Repository interface for FileMetadata entity operations.
/// </summary>
public interface IFileMetadataRepository
{
    Task<FileMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FileMetadata> AddAsync(FileMetadata file, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentAttachment> SavePaymentAttachmentAsync(PaymentAttachment attachment, CancellationToken cancellationToken = default);
}

using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> b)
    {
        b.ToTable("Files");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.StorageProvider)
            .IsRequired();

        b.Property(x => x.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        b.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        b.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.SizeBytes)
            .IsRequired();

        b.Property(x => x.Sha256)
            .HasMaxLength(64);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany(x => x.Files)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.StorageKey);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

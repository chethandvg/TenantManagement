using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class TenantDocumentConfiguration : IEntityTypeConfiguration<TenantDocument>
{
    public void Configure(EntityTypeBuilder<TenantDocument> b)
    {
        b.ToTable("TenantDocuments");
        b.HasKey(x => x.Id);

        b.Property(x => x.TenantId)
            .IsRequired();

        b.Property(x => x.DocType)
            .IsRequired();

        b.Property(x => x.DocNumberMasked)
            .HasMaxLength(50);

        b.Property(x => x.FileId)
            .IsRequired();

        b.Property(x => x.Notes)
            .HasMaxLength(200);

        // Relationships
        b.HasOne(x => x.Tenant)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Lease)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.File)
            .WithMany(x => x.TenantDocuments)
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.DocType);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

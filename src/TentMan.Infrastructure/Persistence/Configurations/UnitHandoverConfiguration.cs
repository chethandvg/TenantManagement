using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UnitHandoverConfiguration : IEntityTypeConfiguration<UnitHandover>
{
    public void Configure(EntityTypeBuilder<UnitHandover> b)
    {
        b.ToTable("UnitHandovers");
        b.HasKey(x => x.Id);

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.Type)
            .IsRequired();

        b.Property(x => x.Date)
            .IsRequired();

        b.Property(x => x.SignedByTenant)
            .IsRequired()
            .HasDefaultValue(false);

        b.Property(x => x.SignedByOwner)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        b.HasOne(x => x.Lease)
            .WithMany(x => x.Handovers)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.SignatureTenantFile)
            .WithMany()
            .HasForeignKey(x => x.SignatureTenantFileId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne(x => x.SignatureOwnerFile)
            .WithMany()
            .HasForeignKey(x => x.SignatureOwnerFileId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.Type);
        b.HasIndex(x => x.Date);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

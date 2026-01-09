using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class LeasePartyConfiguration : IEntityTypeConfiguration<LeaseParty>
{
    public void Configure(EntityTypeBuilder<LeaseParty> b)
    {
        b.ToTable("LeaseParties");
        b.HasKey(x => x.Id);

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.TenantId)
            .IsRequired();

        b.Property(x => x.Role)
            .IsRequired();

        b.Property(x => x.IsResponsibleForPayment)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        b.HasOne(x => x.Lease)
            .WithMany(x => x.Parties)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Tenant)
            .WithMany(x => x.LeaseParties)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: Each tenant can only be listed once per lease
        b.HasIndex(x => new { x.LeaseId, x.TenantId }).IsUnique();

        // Indexes
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.Role);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

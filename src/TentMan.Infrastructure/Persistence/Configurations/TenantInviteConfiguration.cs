using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class TenantInviteConfiguration : IEntityTypeConfiguration<TenantInvite>
{
    public void Configure(EntityTypeBuilder<TenantInvite> b)
    {
        b.ToTable("TenantInvites");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.TenantId)
            .IsRequired();

        b.Property(x => x.InviteToken)
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.Phone)
            .HasMaxLength(15)
            .IsRequired();

        b.Property(x => x.Email)
            .HasMaxLength(254);

        b.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        b.Property(x => x.IsUsed)
            .IsRequired();

        b.Property(x => x.UsedAtUtc);

        b.Property(x => x.AcceptedByUserId);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AcceptedByUser)
            .WithMany()
            .HasForeignKey(x => x.AcceptedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.InviteToken).IsUnique();
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.ExpiresAtUtc);
        b.HasIndex(x => x.IsUsed);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

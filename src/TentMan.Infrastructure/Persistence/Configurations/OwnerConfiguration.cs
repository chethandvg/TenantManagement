using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> b)
    {
        b.ToTable("Owners");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.OwnerType)
            .IsRequired();

        b.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Phone)
            .HasMaxLength(20)
            .IsRequired();

        b.Property(x => x.Email)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Pan)
            .HasMaxLength(10);

        b.Property(x => x.Gstin)
            .HasMaxLength(15);

        b.Property(x => x.LinkedUserId);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany(x => x.Owners)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.LinkedUser)
            .WithMany()
            .HasForeignKey(x => x.LinkedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.Phone);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

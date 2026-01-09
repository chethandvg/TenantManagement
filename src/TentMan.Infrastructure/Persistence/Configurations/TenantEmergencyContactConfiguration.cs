using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class TenantEmergencyContactConfiguration : IEntityTypeConfiguration<TenantEmergencyContact>
{
    public void Configure(EntityTypeBuilder<TenantEmergencyContact> b)
    {
        b.ToTable("TenantEmergencyContacts");
        b.HasKey(x => x.Id);

        b.Property(x => x.TenantId)
            .IsRequired();

        b.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        b.Property(x => x.Relationship)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.Phone)
            .HasMaxLength(15)
            .IsRequired();

        b.Property(x => x.Email)
            .HasMaxLength(254);

        // Relationships
        b.HasOne(x => x.Tenant)
            .WithMany(x => x.EmergencyContacts)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        b.HasIndex(x => x.TenantId);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

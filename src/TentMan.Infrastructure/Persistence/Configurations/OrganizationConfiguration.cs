using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> b)
    {
        b.ToTable("Organizations");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.TimeZone)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.IsActive)
            .IsRequired();

        // Indexes
        b.HasIndex(x => x.Name);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

using Archu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archu.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Price)
            .HasPrecision(18, 2);

        // Indexes for read performance
        b.HasIndex(x => x.Name);

        // Concurrency token is on BaseEntity.RowVersion via [Timestamp]
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
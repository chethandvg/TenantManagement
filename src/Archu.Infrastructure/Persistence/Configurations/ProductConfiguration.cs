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

        // Owner relationship
        b.Property(x => x.OwnerId)
            .IsRequired();

        // Foreign key to Users table
        b.HasOne<Archu.Domain.Entities.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        // Indexes for read performance
        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.OwnerId); // Index for filtering by owner

        // Concurrency token is on BaseEntity.RowVersion via [Timestamp]
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UnitFileConfiguration : IEntityTypeConfiguration<UnitFile>
{
    public void Configure(EntityTypeBuilder<UnitFile> b)
    {
        b.ToTable("UnitFiles");
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitId)
            .IsRequired();

        b.Property(x => x.FileId)
            .IsRequired();

        b.Property(x => x.FileTag)
            .IsRequired();

        b.Property(x => x.SortOrder)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Unit)
            .WithMany(x => x.UnitFiles)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.File)
            .WithMany(x => x.UnitFiles)
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.UnitId);
        b.HasIndex(x => x.FileId);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

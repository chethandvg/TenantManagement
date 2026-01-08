using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class BuildingFileConfiguration : IEntityTypeConfiguration<BuildingFile>
{
    public void Configure(EntityTypeBuilder<BuildingFile> b)
    {
        b.ToTable("BuildingFiles");
        b.HasKey(x => x.Id);

        b.Property(x => x.BuildingId)
            .IsRequired();

        b.Property(x => x.FileId)
            .IsRequired();

        b.Property(x => x.FileTag)
            .IsRequired();

        b.Property(x => x.SortOrder)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Building)
            .WithMany(x => x.BuildingFiles)
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.File)
            .WithMany(x => x.BuildingFiles)
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.BuildingId);
        b.HasIndex(x => x.FileId);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

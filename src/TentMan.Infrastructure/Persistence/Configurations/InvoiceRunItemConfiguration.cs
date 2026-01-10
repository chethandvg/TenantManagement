using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class InvoiceRunItemConfiguration : IEntityTypeConfiguration<InvoiceRunItem>
{
    public void Configure(EntityTypeBuilder<InvoiceRunItem> b)
    {
        b.ToTable("InvoiceRunItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceRunId)
            .IsRequired();

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.IsSuccess)
            .IsRequired()
            .HasDefaultValue(false);

        b.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        // Relationships
        b.HasOne(x => x.InvoiceRun)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InvoiceRunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Lease)
            .WithMany()
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.InvoiceRunId);
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.InvoiceId);
        b.HasIndex(x => new { x.InvoiceRunId, x.LeaseId })
            .IsUnique();

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

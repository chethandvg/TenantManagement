using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> b)
    {
        b.ToTable("CreditNotes");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.InvoiceId)
            .IsRequired();

        b.Property(x => x.CreditNoteNumber)
            .IsRequired()
            .HasMaxLength(50);

        b.Property(x => x.CreditNoteDate)
            .IsRequired();

        b.Property(x => x.Reason)
            .IsRequired();

        b.Property(x => x.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Invoice)
            .WithMany(x => x.CreditNotes)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.CreditNoteNumber)
            .IsUnique();
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.InvoiceId);
        b.HasIndex(x => x.CreditNoteDate);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

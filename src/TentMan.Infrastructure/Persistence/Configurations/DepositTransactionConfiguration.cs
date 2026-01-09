using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class DepositTransactionConfiguration : IEntityTypeConfiguration<DepositTransaction>
{
    public void Configure(EntityTypeBuilder<DepositTransaction> b)
    {
        b.ToTable("DepositTransactions");
        b.HasKey(x => x.Id);

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.TxnType)
            .IsRequired();

        b.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(x => x.TxnDate)
            .IsRequired();

        b.Property(x => x.Reference)
            .HasMaxLength(100);

        b.Property(x => x.Notes)
            .HasMaxLength(300);

        // Relationships
        b.HasOne(x => x.Lease)
            .WithMany(x => x.DepositTransactions)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.TxnDate);
        b.HasIndex(x => x.TxnType);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

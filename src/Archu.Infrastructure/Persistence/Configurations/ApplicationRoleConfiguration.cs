using Archu.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for ApplicationRole.
/// Configures database schema, indexes, and relationships for role-based authorization.
/// </summary>
public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        // Role name
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.NormalizedName)
            .IsRequired()
            .HasMaxLength(256);

        // Description
        builder.Property(r => r.Description)
            .HasMaxLength(500);

        // Indexes for performance
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Roles_NormalizedName");

        // Concurrency token (inherited from BaseEntity)
        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        // Relationships
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

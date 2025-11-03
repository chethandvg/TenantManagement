using Archu.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the schema for <see cref="ApplicationPermission"/> entities including constraints and indexes.
/// </summary>
public sealed class ApplicationPermissionConfiguration : IEntityTypeConfiguration<ApplicationPermission>
{
    /// <summary>
    /// Applies relational mapping rules for the <see cref="ApplicationPermission"/> entity.
    /// </summary>
    /// <param name="builder">Entity type builder supplied by Entity Framework Core.</param>
    public void Configure(EntityTypeBuilder<ApplicationPermission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(permission => permission.NormalizedName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(permission => permission.Description)
            .HasMaxLength(512);

        builder.HasIndex(permission => permission.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Permissions_NormalizedName");

        builder.Property(permission => permission.RowVersion)
            .IsRowVersion();
    }
}

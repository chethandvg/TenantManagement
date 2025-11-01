using Archu.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for the <see cref="UserPermission"/> junction table.
/// Applies relational constraints to manage direct user-permission assignments.
/// </summary>
public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    /// <summary>
    /// Configures the <see cref="UserPermission"/> schema including keys, indexes, and relationships.
    /// Provides deterministic behavior for granting and revoking explicit permissions.
    /// </summary>
    /// <param name="builder">The EF Core entity builder.</param>
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");

        builder.HasKey(up => up.Id);

        builder.HasIndex(up => new { up.UserId, up.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_UserPermissions_UserId_PermissionId");

        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(up => up.RowVersion)
            .IsRowVersion();
    }
}

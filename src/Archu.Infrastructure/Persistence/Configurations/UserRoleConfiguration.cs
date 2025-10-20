using Archu.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for UserRole junction table.
/// Configures the many-to-many relationship between users and roles.
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        // Composite primary key
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        // Audit tracking
        builder.Property(ur => ur.AssignedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ur => ur.AssignedBy)
            .HasMaxLength(256);

        // Indexes for performance
        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UserRoles_UserId");

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");

        // Relationships configured in ApplicationUserConfiguration and ApplicationRoleConfiguration
    }
}

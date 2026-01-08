using TentMan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for ApplicationUser.
/// Configures database schema, indexes, and relationships for user authentication.
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // Username
        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(256);

        // Email
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        // Password
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        // Security
        builder.Property(u => u.SecurityStamp)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(512);

        // Phone
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50);

        // Indexes for performance
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("IX_Users_NormalizedEmail");

        builder.HasIndex(u => u.UserName)
            .IsUnique()
            .HasDatabaseName("IX_Users_UserName");

        // Concurrency token (inherited from BaseEntity)
        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        // Relationships
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

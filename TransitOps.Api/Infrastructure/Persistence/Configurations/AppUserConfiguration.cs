using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitOps.Api.Domain.Entities;

namespace TransitOps.Api.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable(
            "app_user",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_app_user_role_valid",
                    "\"user_role\" IN (0, 1)");
            });

        builder.HasKey(appUser => appUser.Id);

        builder.Property(appUser => appUser.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(appUser => appUser.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(appUser => appUser.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(appUser => appUser.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(appUser => appUser.UserRole)
            .HasColumnName("user_role")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(appUser => appUser.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(appUser => appUser.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(appUser => appUser.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(appUser => appUser.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp");

        builder.HasIndex(appUser => appUser.Username)
            .HasDatabaseName("ux_app_user_username_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(appUser => appUser.Email)
            .HasDatabaseName("ux_app_user_email_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(appUser => appUser.DeletedAt)
            .HasDatabaseName("ix_app_user_deleted_at");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitOps.Api.Domain.Entities;

namespace TransitOps.Api.Persistence.Configurations;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("driver");

        builder.HasKey(driver => driver.Id);

        builder.Property(driver => driver.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(driver => driver.EmployeeCode)
            .HasColumnName("employee_code")
            .HasMaxLength(100);

        builder.Property(driver => driver.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(driver => driver.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(driver => driver.LicenseNumber)
            .HasColumnName("license_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(driver => driver.LicenseExpiryDate)
            .HasColumnName("license_expiry_date");

        builder.Property(driver => driver.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(driver => driver.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(driver => driver.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(driver => driver.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(driver => driver.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(driver => driver.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp");

        builder.HasIndex(driver => driver.LicenseNumber)
            .HasDatabaseName("ux_driver_license_number_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(driver => driver.EmployeeCode)
            .HasDatabaseName("ux_driver_employee_code_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(driver => driver.Email)
            .HasDatabaseName("ux_driver_email_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(driver => driver.DeletedAt)
            .HasDatabaseName("ix_driver_deleted_at");
    }
}

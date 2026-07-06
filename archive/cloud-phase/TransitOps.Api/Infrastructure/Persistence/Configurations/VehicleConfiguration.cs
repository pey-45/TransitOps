using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitOps.Api.Domain.Entities;

namespace TransitOps.Api.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable(
            "vehicle",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_vehicle_capacity_kg_non_negative",
                    "\"capacity_kg\" IS NULL OR \"capacity_kg\" >= 0");

                tableBuilder.HasCheckConstraint(
                    "ck_vehicle_capacity_m3_non_negative",
                    "\"capacity_m3\" IS NULL OR \"capacity_m3\" >= 0");
            });

        builder.HasKey(vehicle => vehicle.Id);

        builder.Property(vehicle => vehicle.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(vehicle => vehicle.PlateNumber)
            .HasColumnName("plate_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(vehicle => vehicle.InternalCode)
            .HasColumnName("internal_code")
            .HasMaxLength(100);

        builder.Property(vehicle => vehicle.Brand)
            .HasColumnName("brand")
            .HasMaxLength(100);

        builder.Property(vehicle => vehicle.Model)
            .HasColumnName("model")
            .HasMaxLength(100);

        builder.Property(vehicle => vehicle.CapacityKg)
            .HasColumnName("capacity_kg")
            .HasPrecision(12, 2);

        builder.Property(vehicle => vehicle.CapacityM3)
            .HasColumnName("capacity_m3")
            .HasPrecision(12, 2);

        builder.Property(vehicle => vehicle.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(vehicle => vehicle.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(vehicle => vehicle.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(vehicle => vehicle.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp");

        builder.HasIndex(vehicle => vehicle.PlateNumber)
            .HasDatabaseName("ux_vehicle_plate_number_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(vehicle => vehicle.InternalCode)
            .HasDatabaseName("ux_vehicle_internal_code_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(vehicle => vehicle.DeletedAt)
            .HasDatabaseName("ix_vehicle_deleted_at");
    }
}

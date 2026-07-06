using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitOps.Api.Domain.Entities;

namespace TransitOps.Api.Infrastructure.Persistence.Configurations;

public sealed class TransportConfiguration : IEntityTypeConfiguration<Transport>
{
    public void Configure(EntityTypeBuilder<Transport> builder)
    {
        builder.ToTable(
            "transport",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_transport_planned_dates",
                    "\"planned_delivery_at\" IS NULL OR \"planned_delivery_at\" >= \"planned_pickup_at\"");

                tableBuilder.HasCheckConstraint(
                    "ck_transport_actual_dates",
                    "\"actual_pickup_at\" IS NULL OR \"actual_delivery_at\" IS NULL OR \"actual_delivery_at\" >= \"actual_pickup_at\"");

                tableBuilder.HasCheckConstraint(
                    "ck_transport_status_valid",
                    "\"status\" IN (0, 1, 2, 3)");
            });

        builder.HasKey(transport => transport.Id);

        builder.Property(transport => transport.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(transport => transport.Reference)
            .HasColumnName("reference")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(transport => transport.Description)
            .HasColumnName("description");

        builder.Property(transport => transport.Origin)
            .HasColumnName("origin")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(transport => transport.Destination)
            .HasColumnName("destination")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(transport => transport.PlannedPickupAt)
            .HasColumnName("planned_pickup_at")
            .HasColumnType("timestamp")
            .IsRequired();

        builder.Property(transport => transport.PlannedDeliveryAt)
            .HasColumnName("planned_delivery_at")
            .HasColumnType("timestamp");

        builder.Property(transport => transport.ActualPickupAt)
            .HasColumnName("actual_pickup_at")
            .HasColumnType("timestamp");

        builder.Property(transport => transport.ActualDeliveryAt)
            .HasColumnName("actual_delivery_at")
            .HasColumnType("timestamp");

        builder.Property(transport => transport.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(transport => transport.VehicleId)
            .HasColumnName("vehicle_id");

        builder.Property(transport => transport.DriverId)
            .HasColumnName("driver_id");

        builder.Property(transport => transport.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(transport => transport.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(transport => transport.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp");

        builder.HasIndex(transport => transport.Reference)
            .HasDatabaseName("ux_transport_reference_active")
            .IsUnique()
            .HasFilter("\"deleted_at\" IS NULL");

        builder.HasIndex(transport => transport.VehicleId)
            .HasDatabaseName("ix_transport_vehicle_id");

        builder.HasIndex(transport => transport.DriverId)
            .HasDatabaseName("ix_transport_driver_id");

        builder.HasIndex(transport => transport.Status)
            .HasDatabaseName("ix_transport_status");

        builder.HasIndex(transport => transport.DeletedAt)
            .HasDatabaseName("ix_transport_deleted_at");

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(transport => transport.VehicleId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_transport_vehicle");

        builder.HasOne<Driver>()
            .WithMany()
            .HasForeignKey(transport => transport.DriverId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_transport_driver");
    }
}

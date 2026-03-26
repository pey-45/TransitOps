using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitOps.Api.Domain.Entities;

namespace TransitOps.Api.Persistence.Configurations;

public sealed class ShipmentEventConfiguration : IEntityTypeConfiguration<ShipmentEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentEvent> builder)
    {
        builder.ToTable("shipment_event");

        builder.HasKey(shipmentEvent => shipmentEvent.Id);

        builder.Property(shipmentEvent => shipmentEvent.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(shipmentEvent => shipmentEvent.TransportId)
            .HasColumnName("transport_id")
            .IsRequired();

        builder.Property(shipmentEvent => shipmentEvent.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(shipmentEvent => shipmentEvent.EventType)
            .HasColumnName("event_type")
            .HasColumnType("shipment_event_type")
            .IsRequired();

        builder.Property(shipmentEvent => shipmentEvent.EventDate)
            .HasColumnName("event_date")
            .HasColumnType("timestamp")
            .IsRequired();

        builder.Property(shipmentEvent => shipmentEvent.Location)
            .HasColumnName("location")
            .HasMaxLength(255);

        builder.Property(shipmentEvent => shipmentEvent.Notes)
            .HasColumnName("notes");

        builder.Property(shipmentEvent => shipmentEvent.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(shipmentEvent => shipmentEvent.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp");

        builder.HasIndex(shipmentEvent => shipmentEvent.TransportId)
            .HasDatabaseName("ix_shipment_event_transport_id");

        builder.HasIndex(shipmentEvent => shipmentEvent.CreatedByUserId)
            .HasDatabaseName("ix_shipment_event_created_by_user_id");

        builder.HasIndex(shipmentEvent => shipmentEvent.EventDate)
            .HasDatabaseName("ix_shipment_event_date");

        builder.HasIndex(shipmentEvent => shipmentEvent.DeletedAt)
            .HasDatabaseName("ix_shipment_event_deleted_at");

        builder.HasOne<Transport>()
            .WithMany()
            .HasForeignKey(shipmentEvent => shipmentEvent.TransportId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shipment_event_transport");

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(shipmentEvent => shipmentEvent.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_shipment_event_created_by_user");
    }
}

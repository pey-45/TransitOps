using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Domain;

namespace TransitOps.Api.Persistence;

public sealed class TransitOpsDbContext(DbContextOptions<TransitOpsDbContext> options) : DbContext(options)
{
    public const string OpenShipmentVehicleIndex = "UX_shipments_open_VehicleId";
    public const string OpenShipmentDriverIndex = "UX_shipments_open_DriverId";

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AppUser>();
        user.ToTable("app_users");
        user.HasKey(item => item.Id);
        user.Property(item => item.Username).HasMaxLength(80).IsRequired();
        user.Property(item => item.Email).HasMaxLength(254).IsRequired();
        user.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
        user.Property(item => item.Role).HasConversion<short>();
        user.HasIndex(item => item.Username).IsUnique();
        user.HasIndex(item => item.Email).IsUnique();

        var vehicle = modelBuilder.Entity<Vehicle>();
        vehicle.ToTable("vehicles");
        vehicle.HasKey(item => item.Id);
        vehicle.Property(item => item.LicensePlate).HasMaxLength(20).IsRequired();
        vehicle.Property(item => item.InternalCode).HasMaxLength(50);
        vehicle.Property(item => item.Brand).HasMaxLength(80);
        vehicle.Property(item => item.Model).HasMaxLength(80);
        vehicle.Property(item => item.LoadCapacity).HasPrecision(12, 2);
        vehicle.HasIndex(item => item.LicensePlate).IsUnique().HasFilter("\"IsActive\"");
        vehicle.HasIndex(item => item.InternalCode).IsUnique()
            .HasFilter("\"IsActive\" AND \"InternalCode\" IS NOT NULL");

        var driver = modelBuilder.Entity<Driver>();
        driver.ToTable("drivers");
        driver.HasKey(item => item.Id);
        driver.Property(item => item.Name).HasMaxLength(160).IsRequired();
        driver.Property(item => item.LicenseNumber).HasMaxLength(50).IsRequired();
        driver.Property(item => item.EmployeeCode).HasMaxLength(50);
        driver.Property(item => item.ContactDetails).HasMaxLength(500);
        driver.HasIndex(item => item.LicenseNumber).IsUnique().HasFilter("\"IsActive\"");

        var customer = modelBuilder.Entity<Customer>();
        customer.ToTable("customers");
        customer.HasKey(item => item.Id);
        customer.Property(item => item.Name).HasMaxLength(160).IsRequired();
        customer.Property(item => item.ContactDetails).HasMaxLength(500);

        var shipment = modelBuilder.Entity<Shipment>();
        shipment.ToTable("shipments", table =>
        {
            table.HasCheckConstraint("ck_shipments_planned_dates", "\"PlannedDeliveryAt\" IS NULL OR \"PlannedDeliveryAt\" >= \"PlannedPickupAt\"");
            table.HasCheckConstraint("ck_shipments_actual_dates", "\"ActualDeliveryAt\" IS NULL OR \"ActualPickupAt\" IS NULL OR \"ActualDeliveryAt\" >= \"ActualPickupAt\"");
        });
        shipment.HasKey(item => item.Id);
        shipment.Property(item => item.Reference).HasMaxLength(50).IsRequired();
        shipment.Property(item => item.Origin).HasMaxLength(160).IsRequired();
        shipment.Property(item => item.Destination).HasMaxLength(160).IsRequired();
        shipment.Property(item => item.Notes).HasMaxLength(500);
        shipment.Property(item => item.EstimatedLoad).HasPrecision(12, 2);
        shipment.Property(item => item.Status).HasConversion<short>();
        shipment.HasIndex(item => item.Reference).IsUnique();
        shipment.HasIndex(item => item.Status);
        shipment.HasIndex(item => item.PlannedPickupAt);
        shipment.HasIndex(item => item.VehicleId)
            .IsUnique()
            .HasDatabaseName(OpenShipmentVehicleIndex)
            .HasFilter("\"VehicleId\" IS NOT NULL AND \"Status\" IN (0, 1)");
        shipment.HasIndex(item => item.DriverId)
            .IsUnique()
            .HasDatabaseName(OpenShipmentDriverIndex)
            .HasFilter("\"DriverId\" IS NOT NULL AND \"Status\" IN (0, 1)");
        shipment.HasOne(item => item.Customer).WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
        shipment.HasOne<Vehicle>().WithMany().HasForeignKey(item => item.VehicleId).OnDelete(DeleteBehavior.Restrict);
        shipment.HasOne<Driver>().WithMany().HasForeignKey(item => item.DriverId).OnDelete(DeleteBehavior.Restrict);

        var shipmentEvent = modelBuilder.Entity<ShipmentEvent>();
        shipmentEvent.ToTable("shipment_events");
        shipmentEvent.HasKey(item => item.Id);
        shipmentEvent.Property(item => item.EventType).HasConversion<short>();
        shipmentEvent.Property(item => item.Location).HasMaxLength(160);
        shipmentEvent.Property(item => item.Notes).HasMaxLength(500);
        shipmentEvent.HasIndex(item => new { item.ShipmentId, item.OccurredAt });
        shipmentEvent.HasIndex(item => item.EventType);
        shipmentEvent.HasOne(item => item.Shipment).WithMany()
            .HasForeignKey(item => item.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
        shipmentEvent.HasOne(item => item.RecordedByUser).WithMany()
            .HasForeignKey(item => item.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

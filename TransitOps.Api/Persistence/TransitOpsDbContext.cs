using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Persistence.Configurations;

namespace TransitOps.Api.Persistence;

public sealed class TransitOpsDbContext : DbContext
{
    public TransitOpsDbContext(DbContextOptions<TransitOpsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Transport> Transports => Set<Transport>();

    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresEnum<TransportStatus>("transport_status");
        modelBuilder.HasPostgresEnum<ShipmentEventType>("shipment_event_type");
        modelBuilder.HasPostgresEnum<UserRole>("user_role");

        modelBuilder.ApplyConfiguration(new AppUserConfiguration());
        modelBuilder.ApplyConfiguration(new DriverConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new TransportConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentEventConfiguration());
    }
}

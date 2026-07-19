using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Domain;

namespace TransitOps.Api.Persistence;

public sealed class TransitOpsDbContext(DbContextOptions<TransitOpsDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Customer> Customers => Set<Customer>();

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
    }
}

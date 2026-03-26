using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Persistence;

public sealed class TransitOpsDbContextFactory : IDesignTimeDbContextFactory<TransitOpsDbContext>
{
    public TransitOpsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<TransitOpsDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.MapEnum<TransportStatus>("transport_status");
                npgsqlOptions.MapEnum<ShipmentEventType>("shipment_event_type");
                npgsqlOptions.MapEnum<UserRole>("user_role");
            });

        return new TransitOpsDbContext(optionsBuilder.Options);
    }
}

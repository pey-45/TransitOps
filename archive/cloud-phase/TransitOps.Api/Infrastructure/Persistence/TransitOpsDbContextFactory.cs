using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TransitOps.Api.Infrastructure.Persistence;

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
        optionsBuilder.UseNpgsql(connectionString);

        return new TransitOpsDbContext(optionsBuilder.Options);
    }
}

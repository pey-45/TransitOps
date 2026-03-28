using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TransitOps.Api;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests;

public sealed class TransitOpsApiFactory : WebApplicationFactory<Program>
{
    private readonly Action<TransitOpsDbContext>? _seed;
    private readonly string _databaseName = $"transitops-tests-{Guid.NewGuid():N}";

    public TransitOpsApiFactory(Action<TransitOpsDbContext>? seed = null)
    {
        _seed = seed;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<TransitOpsDbContext>>();
            services.RemoveAll<DbContextOptions<TransitOpsDbContext>>();
            services.RemoveAll<TransitOpsDbContext>();

            services.AddDbContext<TransitOpsDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            _seed?.Invoke(dbContext);
            dbContext.SaveChanges();
        });
    }
}

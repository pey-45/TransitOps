using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TransitOps.Api;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Support;

public sealed class TransitOpsApiFactory(Action<TransitOpsDbContext>? seed = null) : WebApplicationFactory<Program>
{
    public const string BootstrapToken = "development-bootstrap-token";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = $"transitops-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=transitops_tests");
        builder.UseSetting("Jwt:Issuer", "TransitOps.Tests");
        builder.UseSetting("Jwt:Audience", "TransitOps.Tests");
        builder.UseSetting("Jwt:SigningKey", "controller-test-signing-key-with-at-least-32-characters");
        builder.UseSetting("Bootstrap:FirstAdminToken", BootstrapToken);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TransitOpsDbContext>>();
            services.RemoveAll<TransitOpsDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<TransitOpsDbContext>>();
            services.AddDbContext<TransitOpsDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        db.Database.EnsureCreated();
        seed?.Invoke(db);
        db.SaveChanges();
        return host;
    }

    public static AppUser CreateUser(string username, string password, UserRole role, bool active = true)
    {
        var user = new AppUser
        {
            Username = username,
            Email = $"{username}@transitops.test",
            PasswordHash = "",
            Role = role,
            IsActive = active
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        return user;
    }
}

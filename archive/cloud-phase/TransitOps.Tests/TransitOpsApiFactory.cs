using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TransitOps.Api;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Tests;

public sealed class TransitOpsApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid DefaultAuthenticatedUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string DefaultAuthenticatedUsername = "test.admin";
    public const string DefaultAuthenticatedEmail = "test.admin@transitops.test";
    public const string TestJwtIssuer = "TransitOps.Tests";
    public const string TestJwtAudience = "TransitOps.Tests.Clients";
    public const string TestJwtSigningKey = "TransitOps.Tests.SigningKey.012345678901234567890123456789";
    public const string TestBootstrapToken = "transitops-tests-bootstrap-token";

    private readonly Action<TransitOpsDbContext>? _seed;
    private readonly bool _useTestAuthentication;
    private readonly Guid _authenticatedUserId;
    private readonly string _authenticatedUsername;
    private readonly string _authenticatedEmail;
    private readonly UserRole _authenticatedRole;
    private readonly string _databaseName = $"transitops-tests-{Guid.NewGuid():N}";

    public TransitOpsApiFactory(
        Action<TransitOpsDbContext>? seed = null,
        bool useTestAuthentication = true,
        Guid? authenticatedUserId = null,
        string authenticatedUsername = DefaultAuthenticatedUsername,
        string authenticatedEmail = DefaultAuthenticatedEmail,
        UserRole authenticatedRole = UserRole.Admin)
    {
        _seed = seed;
        _useTestAuthentication = useTestAuthentication;
        _authenticatedUserId = authenticatedUserId ?? DefaultAuthenticatedUserId;
        _authenticatedUsername = authenticatedUsername;
        _authenticatedEmail = authenticatedEmail;
        _authenticatedRole = authenticatedRole;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestJwtSigningKey);
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Bootstrap__FirstAdminToken", TestBootstrapToken);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<TransitOpsDbContext>>();
            services.RemoveAll<DbContextOptions<TransitOpsDbContext>>();
            services.RemoveAll<TransitOpsDbContext>();

            services.AddDbContext<TransitOpsDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            if (_useTestAuthentication)
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        options =>
                        {
                            options.UserId = _authenticatedUserId;
                            options.Username = _authenticatedUsername;
                            options.Email = _authenticatedEmail;
                            options.Role = _authenticatedRole;
                        });
            }

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

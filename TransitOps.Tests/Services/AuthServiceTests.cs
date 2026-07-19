using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Auth;
using TransitOps.Api.Persistence;
using TransitOps.Api.Security;

namespace TransitOps.Tests.Services;

public sealed class AuthServiceTests
{
    private const string BootstrapToken = "service-test-bootstrap-token";
    private const string Password = "SecurePass!123";

    [Fact]
    public async Task Bootstrap_creates_normalized_admin_with_hashed_password()
    {
        await using var db = CreateDatabase();
        var service = CreateService(db);

        var response = await service.BootstrapAsync(
            new BootstrapAdminRequest(" first.admin ", "FIRST.ADMIN@TRANSITOPS.TEST ", Password),
            BootstrapToken,
            CancellationToken.None);
        var stored = await db.AppUsers.SingleAsync(CancellationToken.None);

        Assert.Equal("first.admin", response.Username);
        Assert.Equal("first.admin@transitops.test", response.Email);
        Assert.Equal("admin", response.Role);
        Assert.Equal(UserRole.Admin, stored.Role);
        Assert.NotEqual(Password, stored.PasswordHash);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            new PasswordHasher<AppUser>().VerifyHashedPassword(stored, stored.PasswordHash, Password));
    }

    [Fact]
    public async Task Bootstrap_rejects_invalid_bootstrap_token()
    {
        await using var db = CreateDatabase();
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.BootstrapAsync(
            ValidBootstrapRequest(),
            "wrong-token",
            CancellationToken.None));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("invalid_bootstrap_token", exception.Code);
    }

    [Fact]
    public async Task Bootstrap_is_unavailable_when_token_is_not_configured()
    {
        await using var db = CreateDatabase();
        var service = CreateService(db, "");

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.BootstrapAsync(
            ValidBootstrapRequest(),
            null,
            CancellationToken.None));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal("bootstrap_not_configured", exception.Code);
    }

    [Fact]
    public async Task Bootstrap_is_blocked_when_an_active_admin_exists()
    {
        await using var db = CreateDatabase();
        db.AppUsers.Add(CreateUser("existing.admin", Password, UserRole.Admin));
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.BootstrapAsync(
            ValidBootstrapRequest(),
            BootstrapToken,
            CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("first_admin_already_exists", exception.Code);
    }

    [Fact]
    public async Task Bootstrap_rejects_existing_username_or_email()
    {
        await using var db = CreateDatabase();
        db.AppUsers.Add(CreateUser("first.admin", Password, UserRole.Operator));
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.BootstrapAsync(
            ValidBootstrapRequest(),
            BootstrapToken,
            CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("user_credentials_conflict", exception.Code);
    }

    [Fact]
    public async Task Login_returns_signed_token_with_operator_role()
    {
        await using var db = CreateDatabase();
        db.AppUsers.Add(CreateUser("operator", Password, UserRole.Operator));
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var response = await service.LoginAsync(
            new LoginRequest(" operator ", Password),
            CancellationToken.None);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal("operator", response.User.Role);
        Assert.Equal("operator", jwt.Claims.Single(claim => claim.Type == "unique_name").Value);
        Assert.Equal("operator", jwt.Claims.Single(claim => claim.Type == "role").Value);
    }

    [Theory]
    [InlineData("missing", Password, false)]
    [InlineData("operator", "WrongPassword!123", true)]
    [InlineData("operator", Password, false)]
    public async Task Login_rejects_missing_bad_password_or_inactive_user(
        string username,
        string password,
        bool active)
    {
        await using var db = CreateDatabase();
        if (username == "operator")
        {
            db.AppUsers.Add(CreateUser("operator", Password, UserRole.Operator, active));
            await db.SaveChangesAsync(CancellationToken.None);
        }
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(
            new LoginRequest(username, password),
            CancellationToken.None));

        Assert.Equal(401, exception.StatusCode);
        Assert.Equal("invalid_credentials", exception.Code);
    }

    private static TransitOpsDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<TransitOpsDbContext>()
            .UseInMemoryDatabase($"auth-service-tests-{Guid.NewGuid():N}")
            .Options;
        return new TransitOpsDbContext(options);
    }

    private static AuthService CreateService(TransitOpsDbContext db, string bootstrapToken = BootstrapToken) => new(
        db,
        new PasswordHasher<AppUser>(),
        new JwtOptions
        {
            Issuer = "TransitOps.Tests",
            Audience = "TransitOps.Tests",
            SigningKey = "service-test-signing-key-with-at-least-32-characters",
            ExpirationMinutes = 30
        },
        new BootstrapOptions { FirstAdminToken = bootstrapToken });

    private static BootstrapAdminRequest ValidBootstrapRequest() =>
        new("first.admin", "first.admin@transitops.test", Password);

    private static AppUser CreateUser(string username, string password, UserRole role, bool active = true)
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

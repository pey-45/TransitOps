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
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

        Assert.Equal("operator", response.User.Role);
        Assert.Equal("operator", jwt.Claims.Single(claim => claim.Type == "unique_name").Value);
        Assert.Equal("operator", jwt.Claims.Single(claim => claim.Type == "role").Value);
        Assert.Equal("0", jwt.Claims.Single(claim => claim.Type == AuthSession.TokenVersionClaim).Value);
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

    [Fact]
    public async Task Change_password_replaces_the_credential_used_by_login()
    {
        await using var db = CreateDatabase();
        var user = CreateUser("operator", Password, UserRole.Operator);
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, currentUserId: user.Id);

        await service.ChangePasswordAsync(new ChangePasswordRequest(Password, "NewSecurePass!456"), default);

        Assert.Equal(1, user.TokenVersion);

        var oldPassword = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(new LoginRequest("operator", Password), default));
        Assert.Equal("invalid_credentials", oldPassword.Code);
        Assert.Equal("operator", (await service.LoginAsync(
            new LoginRequest("operator", "NewSecurePass!456"),
            default)).User.Username);
    }

    [Fact]
    public async Task Change_password_rejects_a_wrong_current_password_without_changing_it()
    {
        await using var db = CreateDatabase();
        var user = CreateUser("operator", Password, UserRole.Operator);
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, currentUserId: user.Id);

        var error = await Assert.ThrowsAsync<ApiException>(() =>
            service.ChangePasswordAsync(new ChangePasswordRequest("WrongPass!123", "NewSecurePass!456"), default));

        Assert.Equal(401, error.StatusCode);
        Assert.Equal("invalid_credentials", error.Code);
        Assert.Equal("operator", (await service.LoginAsync(new LoginRequest("operator", Password), default)).User.Username);
    }

    [Fact]
    public async Task Change_password_rejects_the_current_password_as_the_new_password()
    {
        await using var db = CreateDatabase();
        var user = CreateUser("operator", Password, UserRole.Operator);
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<ApiException>(() =>
            CreateService(db, currentUserId: user.Id)
                .ChangePasswordAsync(new ChangePasswordRequest(Password, Password), default));

        Assert.Equal(400, error.StatusCode);
        Assert.Equal("password_unchanged", error.Code);
    }

    [Fact]
    public async Task Change_password_rejects_an_inactive_or_missing_current_user()
    {
        await using var db = CreateDatabase();
        var inactive = CreateUser("operator", Password, UserRole.Operator, false);
        db.AppUsers.Add(inactive);
        await db.SaveChangesAsync();

        foreach (var currentUserId in new Guid?[] { inactive.Id, Guid.NewGuid(), null })
        {
            var error = await Assert.ThrowsAsync<ApiException>(() =>
                CreateService(db, currentUserId: currentUserId)
                    .ChangePasswordAsync(new ChangePasswordRequest(Password, "NewSecurePass!456"), default));
            Assert.Equal(401, error.StatusCode);
            Assert.Equal("invalid_credentials", error.Code);
        }
    }

    private static TransitOpsDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<TransitOpsDbContext>()
            .UseInMemoryDatabase($"auth-service-tests-{Guid.NewGuid():N}")
            .Options;
        return new TransitOpsDbContext(options);
    }

    private static AuthService CreateService(
        TransitOpsDbContext db,
        string bootstrapToken = BootstrapToken,
        Guid? currentUserId = null) => new(
        db,
        new PasswordHasher<AppUser>(),
        new JwtOptions
        {
            Issuer = "TransitOps.Tests",
            Audience = "TransitOps.Tests",
            SigningKey = "service-test-signing-key-with-at-least-32-characters",
            ExpirationMinutes = 30
        },
        new BootstrapOptions { FirstAdminToken = bootstrapToken },
        new StubCurrentUser(currentUserId));

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

    private sealed record StubCurrentUser(Guid? Id) : ICurrentUser;
}

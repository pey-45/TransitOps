using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Users;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Services;

public sealed class UserServiceTests
{
    private const string Password = "SecurePass!123";

    [Fact]
    public async Task Create_normalizes_credentials_hashes_password_and_creates_an_active_user()
    {
        await using var db = CreateDatabase();
        var result = await Service(db).CreateAsync(
            new(" operator.one ", "OPERATOR.ONE@TRANSITOPS.TEST ", Password, "operator"),
            default);
        var stored = await db.AppUsers.SingleAsync();

        Assert.Equal("operator.one", result.Username);
        Assert.Equal("operator.one@transitops.test", result.Email);
        Assert.Equal("operator", result.Role);
        Assert.True(result.IsActive);
        Assert.NotEqual(Password, stored.PasswordHash);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            new PasswordHasher<AppUser>().VerifyHashedPassword(stored, stored.PasswordHash, Password));
    }

    [Theory]
    [InlineData("existing", "other@transitops.test")]
    [InlineData("other", "existing@transitops.test")]
    public async Task Create_rejects_global_credential_conflicts_even_when_existing_user_is_inactive(
        string username,
        string email)
    {
        await using var db = CreateDatabase();
        db.AppUsers.Add(User("existing", UserRole.Operator, false));
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<ApiException>(() =>
            Service(db).CreateAsync(new(username, email, Password, "operator"), default));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal("user_credentials_conflict", error.Code);
    }

    [Fact]
    public async Task List_hides_inactive_users_by_default_and_can_include_them()
    {
        await using var db = CreateDatabase();
        db.AppUsers.AddRange(
            User("operator", UserRole.Operator),
            User("admin", UserRole.Admin),
            User("inactive", UserRole.Operator, false));
        await db.SaveChangesAsync();
        var service = Service(db);

        var active = await service.GetAllAsync(new(null), default);
        var all = await service.GetAllAsync(new(true), default);

        Assert.Equal(["admin", "operator"], active.Select(item => item.Username));
        Assert.Equal(3, all.Count);
        Assert.Equal("inactive", (await service.GetByIdAsync(
            (await db.AppUsers.SingleAsync(item => !item.IsActive)).Id,
            default)).Username);
    }

    [Fact]
    public async Task Deactivating_or_demoting_the_last_active_admin_is_protected()
    {
        await using var db = CreateDatabase();
        var admin = User("admin", UserRole.Admin);
        db.AppUsers.Add(admin);
        await db.SaveChangesAsync();
        var service = Service(db);

        foreach (var action in new Func<Task>[]
        {
            () => service.ChangeActivationAsync(admin.Id, new(false), default),
            () => service.ChangeRoleAsync(admin.Id, new("operator"), default)
        })
        {
            var error = await Assert.ThrowsAsync<ApiException>(action);
            Assert.Equal(409, error.StatusCode);
            Assert.Equal("last_admin_protected", error.Code);
        }
    }

    [Fact]
    public async Task Another_active_admin_allows_deactivation_and_demotion()
    {
        await using var db = CreateDatabase();
        var first = User("first", UserRole.Admin);
        var second = User("second", UserRole.Admin);
        db.AppUsers.AddRange(first, second);
        await db.SaveChangesAsync();
        var service = Service(db);

        var deactivated = await service.ChangeActivationAsync(first.Id, new(false), default);
        await service.ChangeActivationAsync(first.Id, new(true), default);
        var demoted = await service.ChangeRoleAsync(second.Id, new("operator"), default);

        Assert.False(deactivated.IsActive);
        Assert.Equal("operator", demoted.Role);
        Assert.Equal(1, first.TokenVersion);
        Assert.Equal(1, second.TokenVersion);
    }

    [Fact]
    public async Task Operators_and_already_inactive_admins_can_change_without_triggering_last_admin_rule()
    {
        await using var db = CreateDatabase();
        var activeAdmin = User("admin", UserRole.Admin);
        var inactiveAdmin = User("old-admin", UserRole.Admin, false);
        var operatorUser = User("operator", UserRole.Operator);
        db.AppUsers.AddRange(activeAdmin, inactiveAdmin, operatorUser);
        await db.SaveChangesAsync();
        var service = Service(db);

        Assert.False((await service.ChangeActivationAsync(operatorUser.Id, new(false), default)).IsActive);
        Assert.Equal("operator", (await service.ChangeRoleAsync(inactiveAdmin.Id, new("operator"), default)).Role);
        Assert.True((await service.ChangeActivationAsync(inactiveAdmin.Id, new(true), default)).IsActive);
    }

    [Fact]
    public async Task Role_and_activation_noops_are_idempotent()
    {
        await using var db = CreateDatabase();
        var admin = User("admin", UserRole.Admin);
        db.AppUsers.Add(admin);
        await db.SaveChangesAsync();
        var updatedAt = admin.UpdatedAt;
        var service = Service(db);

        Assert.Equal("admin", (await service.ChangeRoleAsync(admin.Id, new("admin"), default)).Role);
        Assert.True((await service.ChangeActivationAsync(admin.Id, new(true), default)).IsActive);
        Assert.Equal(updatedAt, (await db.AppUsers.SingleAsync()).UpdatedAt);
    }

    [Fact]
    public async Task Missing_user_is_not_found_for_every_addressed_operation()
    {
        await using var db = CreateDatabase();
        var service = Service(db);
        var id = Guid.NewGuid();

        foreach (var action in new Func<Task>[]
        {
            () => service.GetByIdAsync(id, default),
            () => service.ChangeRoleAsync(id, new("admin"), default),
            () => service.ChangeActivationAsync(id, new(true), default)
        })
        {
            var error = await Assert.ThrowsAsync<ApiException>(action);
            Assert.Equal(404, error.StatusCode);
            Assert.Equal("user_not_found", error.Code);
        }
    }

    private static UserService Service(TransitOpsDbContext db) => new(db, new PasswordHasher<AppUser>());
    private static AppUser User(string username, UserRole role, bool active = true) => new()
    {
        Username = username,
        Email = $"{username}@transitops.test",
        PasswordHash = "hash",
        Role = role,
        IsActive = active
    };
    private static TransitOpsDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<TransitOpsDbContext>()
            .UseInMemoryDatabase($"user-tests-{Guid.NewGuid():N}")
            .Options);
}

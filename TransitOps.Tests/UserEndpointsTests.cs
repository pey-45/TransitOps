using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Tests;

public sealed class UserEndpointsTests
{
    [Fact]
    public async Task GetAll_ReturnsNonDeletedUsers_OrderedByUsername()
    {
        var activeAdmin = CreateUser("admin.a", "admin.a@transitops.dev", "Admin!123", UserRole.Admin);
        var inactiveOperator = CreateUser("operator.b", "operator.b@transitops.dev", "Operator!123", UserRole.Operator, isActive: false);
        var deletedUser = CreateUser(
            "z.deleted",
            "z.deleted@transitops.dev",
            "Deleted!123",
            UserRole.Operator,
            deletedAt: new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.AppUsers.AddRange(activeAdmin, inactiveOperator, deletedUser);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var users = payload["data"]!.AsArray();

        Assert.Equal(2, users.Count);
        Assert.Equal("admin.a", users[0]?["username"]?.GetValue<string>());
        Assert.Equal("operator.b", users[1]?["username"]?.GetValue<string>());
        Assert.False(users[1]?["isActive"]?.GetValue<bool>());
    }

    [Fact]
    public async Task GetById_ReturnsUser_WhenUserExists()
    {
        var appUser = CreateUser("admin.detail", "admin.detail@transitops.dev", "Admin!123", UserRole.Admin);

        using var factory = new TransitOpsApiFactory(dbContext => dbContext.AppUsers.Add(appUser));
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/users/{appUser.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal(appUser.Id.ToString(), payload["data"]?["id"]?.GetValue<string>());
        Assert.Equal(appUser.Username, payload["data"]?["username"]?.GetValue<string>());
        Assert.Equal(appUser.Email, payload["data"]?["email"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndHashesPassword_WhenRequestIsValid()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                username = "managed.admin",
                email = "managed.admin@transitops.dev",
                password = "Managed!123",
                userRole = "admin"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("managed.admin", payload["data"]?["username"]?.GetValue<string>());
        Assert.Equal("managed.admin@transitops.dev", payload["data"]?["email"]?.GetValue<string>());
        Assert.Equal(0, payload["data"]?["userRole"]?.GetValue<int>());
        Assert.True(payload["data"]?["isActive"]?.GetValue<bool>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedUser = await dbContext.AppUsers.SingleAsync(appUser => appUser.Username == "managed.admin");

        Assert.NotEqual("Managed!123", storedUser.PasswordHash);

        var verification = new PasswordHasher<AppUser>()
            .VerifyHashedPassword(storedUser, storedUser.PasswordHash, "Managed!123");

        Assert.NotEqual(PasswordVerificationResult.Failed, verification);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenUsernameAlreadyExists()
    {
        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.AppUsers.Add(CreateUser("managed.admin", "existing@transitops.dev", "Existing!123", UserRole.Admin));
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                username = "managed.admin",
                email = "new@transitops.dev",
                password = "Managed!123",
                userRole = "operator"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("app_user_username_conflict", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task ChangeRole_ReturnsUpdatedUser_WhenTransitionIsAllowed()
    {
        var appUser = CreateUser("managed.operator", "managed.operator@transitops.dev", "Operator!123", UserRole.Operator);
        var existingAdmin = CreateUser("existing.admin", "existing.admin@transitops.dev", "Admin!123", UserRole.Admin);

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.AppUsers.AddRange(existingAdmin, appUser);
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{appUser.Id}/role",
            new
            {
                userRole = "admin"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal(0, payload["data"]?["userRole"]?.GetValue<int>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedUser = await dbContext.AppUsers.SingleAsync(candidate => candidate.Id == appUser.Id);

        Assert.Equal(UserRole.Admin, storedUser.UserRole);
    }

    [Fact]
    public async Task ChangeRole_ReturnsConflict_WhenTargetIsLastActiveAdmin()
    {
        var appUser = CreateUser("solo.admin", "solo.admin@transitops.dev", "Admin!123", UserRole.Admin);

        using var factory = new TransitOpsApiFactory(dbContext => dbContext.AppUsers.Add(appUser));
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{appUser.Id}/role",
            new
            {
                userRole = "operator"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("last_active_admin_role_change_forbidden", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task SetActivation_ReturnsUpdatedUser_WhenTransitionIsAllowed()
    {
        var firstAdmin = CreateUser("admin.one", "admin.one@transitops.dev", "Admin!123", UserRole.Admin);
        var secondAdmin = CreateUser("admin.two", "admin.two@transitops.dev", "Admin!123", UserRole.Admin);

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.AppUsers.AddRange(firstAdmin, secondAdmin);
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{secondAdmin.Id}/activation",
            new
            {
                isActive = false
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.False(payload["data"]?["isActive"]?.GetValue<bool>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedUser = await dbContext.AppUsers.SingleAsync(candidate => candidate.Id == secondAdmin.Id);

        Assert.False(storedUser.IsActive);
    }

    [Fact]
    public async Task SetActivation_ReturnsConflict_WhenTargetIsLastActiveAdmin()
    {
        var appUser = CreateUser("solo.admin", "solo.admin@transitops.dev", "Admin!123", UserRole.Admin);

        using var factory = new TransitOpsApiFactory(dbContext => dbContext.AppUsers.Add(appUser));
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{appUser.Id}/activation",
            new
            {
                isActive = false
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("last_active_admin_deactivation_forbidden", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task UsersEndpoints_ReturnForbidden_ForOperatorRole()
    {
        using var factory = new TransitOpsApiFactory(useTestAuthentication: false);
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateAccessToken(Guid.NewGuid(), "operator.user", "operator.user@transitops.dev", "operator"));

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("authorization_forbidden", payload["error"]?["code"]?.GetValue<string>());
    }

    private static AppUser CreateUser(
        string username,
        string email,
        string password,
        UserRole userRole,
        bool isActive = true,
        DateTime? deletedAt = null)
    {
        var appUser = new AppUser
        {
            Username = username,
            Email = email,
            UserRole = userRole,
            IsActive = isActive,
            CreatedAt = new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc),
            DeletedAt = deletedAt
        };

        appUser.PasswordHash = new PasswordHasher<AppUser>().HashPassword(appUser, password);

        return appUser;
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }

    private static string CreateAccessToken(Guid userId, string username, string email, string role)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TransitOpsApiFactory.TestJwtSigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TransitOpsApiFactory.TestJwtIssuer,
            audience: TransitOpsApiFactory.TestJwtAudience,
            claims: new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("unique_name", username),
                new Claim("email", email),
                new Claim("role", role)
            },
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

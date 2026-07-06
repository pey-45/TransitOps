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

public sealed class AuthEndpointsTests
{
    [Fact]
    public async Task BootstrapFirstAdmin_ReturnsCreatedAndHashesPassword_WhenNoActiveAdminExists()
    {
        using var factory = new TransitOpsApiFactory(useTestAuthentication: false);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Bootstrap-Token", TransitOpsApiFactory.TestBootstrapToken);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/bootstrap-admin",
            new
            {
                username = "bootstrap.admin",
                email = "bootstrap.admin@transitops.dev",
                password = "Bootstrap!123"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("bootstrap.admin", payload["data"]?["username"]?.GetValue<string>());
        Assert.Equal("bootstrap.admin@transitops.dev", payload["data"]?["email"]?.GetValue<string>());
        Assert.Equal(0, payload["data"]?["userRole"]?.GetValue<int>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var appUser = await dbContext.AppUsers.SingleAsync();

        Assert.Equal("bootstrap.admin", appUser.Username);
        Assert.Equal("bootstrap.admin@transitops.dev", appUser.Email);
        Assert.Equal(UserRole.Admin, appUser.UserRole);
        Assert.True(appUser.IsActive);
        Assert.NotEqual("Bootstrap!123", appUser.PasswordHash);

        var passwordHasher = new PasswordHasher<AppUser>();
        var verification = passwordHasher.VerifyHashedPassword(appUser, appUser.PasswordHash, "Bootstrap!123");
        Assert.NotEqual(PasswordVerificationResult.Failed, verification);
    }

    [Fact]
    public async Task BootstrapFirstAdmin_ReturnsConflict_WhenActiveAdminAlreadyExists()
    {
        using var factory = new TransitOpsApiFactory(
            seed: dbContext => dbContext.AppUsers.Add(CreateUser("existing.admin", "existing.admin@transitops.dev", "Existing!123", UserRole.Admin)),
            useTestAuthentication: false);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Bootstrap-Token", TransitOpsApiFactory.TestBootstrapToken);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/bootstrap-admin",
            new
            {
                username = "bootstrap.admin",
                email = "bootstrap.admin@transitops.dev",
                password = "Bootstrap!123"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("first_admin_already_exists", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task BootstrapFirstAdmin_ReturnsUnauthorized_WhenBootstrapTokenIsInvalid()
    {
        using var factory = new TransitOpsApiFactory(useTestAuthentication: false);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Bootstrap-Token", "wrong-token");

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/bootstrap-admin",
            new
            {
                username = "bootstrap.admin",
                email = "bootstrap.admin@transitops.dev",
                password = "Bootstrap!123"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("invalid_bootstrap_token", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreValid()
    {
        using var factory = new TransitOpsApiFactory(
            seed: dbContext => dbContext.AppUsers.Add(CreateUser("auth.admin", "auth.admin@transitops.dev", "Auth!12345", UserRole.Admin)),
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                username = "auth.admin",
                password = "Auth!12345"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var accessToken = payload["data"]?["accessToken"]?.GetValue<string>();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal("Bearer", payload["data"]?["tokenType"]?.GetValue<string>());
        Assert.Equal("auth.admin", payload["data"]?["user"]?["username"]?.GetValue<string>());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal("auth.admin", jwt.Claims.Single(claim => claim.Type == "unique_name").Value);
        Assert.Equal("admin", jwt.Claims.Single(claim => claim.Type == "role").Value);
        Assert.True(Guid.TryParse(jwt.Claims.Single(claim => claim.Type == "sub").Value, out _));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        using var factory = new TransitOpsApiFactory(
            seed: dbContext => dbContext.AppUsers.Add(CreateUser("auth.admin", "auth.admin@transitops.dev", "Auth!12345", UserRole.Admin)),
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                username = "auth.admin",
                password = "WrongPassword!123"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("invalid_credentials", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task ProtectedEndpoint_ReturnsUnauthorized_WhenTokenIsMissing()
    {
        using var factory = new TransitOpsApiFactory(useTestAuthentication: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/transports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("authentication_required", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task ProtectedEndpoint_ReturnsForbidden_WhenRoleIsNotOperational()
    {
        using var factory = new TransitOpsApiFactory(useTestAuthentication: false);
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateAccessToken(Guid.NewGuid(), "guest.user", "guest.user@transitops.dev", "guest"));

        var response = await client.GetAsync("/api/v1/transports");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("authorization_forbidden", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task ProtectedEndpoint_ReturnsOk_WhenUsingTokenIssuedByLogin()
    {
        using var factory = new TransitOpsApiFactory(
            seed: dbContext => dbContext.AppUsers.Add(CreateUser("auth.operator", "auth.operator@transitops.dev", "Auth!12345", UserRole.Operator)),
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                username = "auth.operator",
                password = "Auth!12345"
            });

        var loginPayload = await ReadJsonAsync(loginResponse);
        var accessToken = loginPayload["data"]?["accessToken"]?.GetValue<string>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/transports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static AppUser CreateUser(
        string username,
        string email,
        string password,
        UserRole userRole,
        bool isActive = true)
    {
        var appUser = new AppUser
        {
            Username = username,
            Email = email,
            UserRole = userRole,
            IsActive = isActive,
            CreatedAt = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
        };

        appUser.PasswordHash = new PasswordHasher<AppUser>().HashPassword(appUser, password);

        return appUser;
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

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}

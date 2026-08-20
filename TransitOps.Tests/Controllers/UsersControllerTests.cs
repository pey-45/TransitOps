using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using TransitOps.Api.Domain;
using TransitOps.Tests.Support;

namespace TransitOps.Tests.Controllers;

public sealed class UsersControllerTests
{
    [Theory]
    [InlineData("GET", "/api/v1/users")]
    [InlineData("GET", "/api/v1/users/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/api/v1/users")]
    [InlineData("PUT", "/api/v1/users/11111111-1111-1111-1111-111111111111/role")]
    [InlineData("PUT", "/api/v1/users/11111111-1111-1111-1111-111111111111/activation")]
    [InlineData("PUT", "/api/v1/users/11111111-1111-1111-1111-111111111111/password")]
    public async Task User_administration_requires_an_admin(string method, string path)
    {
        using var factory = FactoryWithOperator();
        using var anonymousRequest = Request(method, path);
        using var anonymousResponse = await factory.CreateClient().SendAsync(anonymousRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal("authentication_required", (await Json(anonymousResponse))["error"]?["code"]?.GetValue<string>());

        using var client = await AuthenticatedClient(factory, "operator", "SecurePass!123");
        using var operatorRequest = Request(method, path);
        using var operatorResponse = await client.SendAsync(operatorRequest);
        Assert.Equal(HttpStatusCode.Forbidden, operatorResponse.StatusCode);
        Assert.Equal("authorization_forbidden", (await Json(operatorResponse))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Admin_can_create_and_list_users_without_exposing_password_hash()
    {
        using var factory = FactoryWithAdmin();
        using var client = await AuthenticatedClient(factory, "admin", "SecurePass!123");

        var createdResponse = await client.PostAsJsonAsync("/api/v1/users", new
        {
            username = " new.operator ",
            email = "NEW.OPERATOR@TRANSITOPS.TEST ",
            password = "OperatorPass!123",
            role = "operator"
        });
        var created = await Json(createdResponse);

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal("new.operator", created["data"]?["username"]?.GetValue<string>());
        Assert.Equal("new.operator@transitops.test", created["data"]?["email"]?.GetValue<string>());
        Assert.Null(created["data"]?["passwordHash"]);
        var list = await Json(await client.GetAsync("/api/v1/users"));
        Assert.Equal(2, list["data"]!.AsArray().Count);
        Assert.All(list["data"]!.AsArray(), item => Assert.Null(item?["passwordHash"]));
    }

    [Fact]
    public async Task Last_admin_protection_is_returned_through_the_common_error_contract()
    {
        var admin = TransitOpsApiFactory.CreateUser("admin", "SecurePass!123", UserRole.Admin);
        using var factory = new TransitOpsApiFactory(db => db.AppUsers.Add(admin));
        using var client = await AuthenticatedClient(factory, "admin", "SecurePass!123");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{admin.Id}/activation",
            new { isActive = false });
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("last_admin_protected", body["error"]?["code"]?.GetValue<string>());
        Assert.Contains("administrador activo", body["error"]?["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task Inactive_users_can_be_listed_and_reactivated()
    {
        var inactive = TransitOpsApiFactory.CreateUser("inactive", "SecurePass!123", UserRole.Operator, false);
        using var factory = FactoryWithAdmin(db => db.AppUsers.Add(inactive));
        using var client = await AuthenticatedClient(factory, "admin", "SecurePass!123");

        Assert.Single((await Json(await client.GetAsync("/api/v1/users")))["data"]!.AsArray());
        Assert.Equal(2, (await Json(await client.GetAsync("/api/v1/users?includeInactive=true")))["data"]!.AsArray().Count);
        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{inactive.Id}/activation",
            new { isActive = true });
        Assert.True((await Json(response))["data"]?["isActive"]?.GetValue<bool>());
    }

    [Theory]
    [InlineData("activation")]
    [InlineData("role")]
    public async Task Deactivation_and_role_changes_invalidate_an_existing_session(string change)
    {
        var admin = TransitOpsApiFactory.CreateUser("admin", "SecurePass!123", UserRole.Admin);
        var operatorUser = TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator);
        using var factory = new TransitOpsApiFactory(db => db.AppUsers.AddRange(admin, operatorUser));
        using var operatorClient = await AuthenticatedClient(factory, "operator", "SecurePass!123");
        using var adminClient = await AuthenticatedClient(factory, "admin", "SecurePass!123");

        var response = change == "activation"
            ? await adminClient.PutAsJsonAsync(
                $"/api/v1/users/{operatorUser.Id}/activation",
                new { isActive = false })
            : await adminClient.PutAsJsonAsync(
                $"/api/v1/users/{operatorUser.Id}/role",
                new { role = "admin" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rejected = await operatorClient.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        Assert.Equal("authentication_required", (await Json(rejected))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Admin_reset_closes_the_target_session_and_enables_the_new_password()
    {
        var admin = TransitOpsApiFactory.CreateUser("admin", "SecurePass!123", UserRole.Admin);
        var operatorUser = TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator);
        using var factory = new TransitOpsApiFactory(db => db.AppUsers.AddRange(admin, operatorUser));
        using var operatorClient = await AuthenticatedClient(factory, "operator", "SecurePass!123");
        using var adminClient = await AuthenticatedClient(factory, "admin", "SecurePass!123");

        var response = await adminClient.PutAsJsonAsync(
            $"/api/v1/users/{operatorUser.Id}/password",
            new { password = "ResetPass!2026" });
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("operator", body["data"]?["username"]?.GetValue<string>());
        Assert.Null(body["data"]?["passwordHash"]);

        var rejected = await operatorClient.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);

        using var reissued = factory.CreateClient();
        var stale = await reissued.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username = "operator", password = "SecurePass!123" });
        Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
        var renewed = await reissued.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username = "operator", password = "ResetPass!2026" });
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_reset_their_own_password_through_administration()
    {
        var admin = TransitOpsApiFactory.CreateUser("admin", "SecurePass!123", UserRole.Admin);
        using var factory = new TransitOpsApiFactory(db => db.AppUsers.Add(admin));
        using var client = await AuthenticatedClient(factory, "admin", "SecurePass!123");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{admin.Id}/password",
            new { password = "ResetPass!2026" });
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("user_self_password_reset", body["error"]?["code"]?.GetValue<string>());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Admin_reset_rejects_a_password_below_the_minimum_length()
    {
        var operatorUser = TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator);
        using var factory = FactoryWithAdmin(db => db.AppUsers.Add(operatorUser));
        using var client = await AuthenticatedClient(factory, "admin", "SecurePass!123");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/users/{operatorUser.Id}/password",
            new { password = "corta" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", (await Json(response))["error"]?["code"]?.GetValue<string>());
    }

    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { });
        return request;
    }

    private static TransitOpsApiFactory FactoryWithOperator() => new(db =>
        db.AppUsers.Add(TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator)));
    private static TransitOpsApiFactory FactoryWithAdmin(Action<TransitOps.Api.Persistence.TransitOpsDbContext>? seed = null) =>
        new(db =>
        {
            db.AppUsers.Add(TransitOpsApiFactory.CreateUser("admin", "SecurePass!123", UserRole.Admin));
            seed?.Invoke(db);
        });
    private static async Task<HttpClient> AuthenticatedClient(
        TransitOpsApiFactory factory,
        string username,
        string password)
    {
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username, password })).EnsureSuccessStatusCode();
        return client;
    }
    private static async Task<JsonNode> Json(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
}

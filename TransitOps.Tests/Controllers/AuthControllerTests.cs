using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using TransitOps.Api.Domain;
using TransitOps.Api.Security;
using TransitOps.Tests.Support;

namespace TransitOps.Tests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Bootstrap_returns_created_with_common_response_contract()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Bootstrap-Token", TransitOpsApiFactory.BootstrapToken);

        var response = await client.PostAsJsonAsync("/api/v1/auth/bootstrap-admin", new
        {
            username = "first.admin",
            email = "first.admin@transitops.test",
            password = "SecurePass!123"
        });
        var payload = await ReadJson(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("admin", payload["data"]?["role"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(payload["requestId"]?.GetValue<string>()));
    }

    [Fact]
    public async Task Login_sets_an_http_only_strict_cookie_without_exposing_the_token()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();

        var response = await Login(client);
        var payload = await ReadJson(response);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(payload["data"]?["accessToken"]);
        Assert.Null(payload["data"]?["tokenType"]);
        Assert.Equal("operator", payload["data"]?["user"]?["role"]?.GetValue<string>());
        Assert.Contains($"{AuthSession.CookieName}=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(payload["requestId"]?.GetValue<string>()));
    }

    [Fact]
    public async Task Login_validation_error_uses_common_error_contract()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "", password = "" });
        var payload = await ReadJson(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", payload["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(payload["error"]?["details"]);
    }

    [Fact]
    public async Task Me_returns_unauthorized_contract_without_session_cookie()
    {
        using var factory = new TransitOpsApiFactory();
        using var response = await factory.CreateClient().GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("authentication_required", (await ReadJson(response))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Operator_can_change_own_password_and_login_with_the_new_password()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();
        (await Login(client)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/auth/password", new
        {
            currentPassword = "SecurePass!123",
            newPassword = "NewSecurePass!456"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await ReadJson(response))["data"]?["changed"]?.GetValue<bool>());
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        var newLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "operator",
            password = "NewSecurePass!456"
        });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task Change_password_rejects_an_incorrect_current_password()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();
        (await Login(client)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/v1/auth/password", new
        {
            currentPassword = "WrongPassword!123",
            newPassword = "NewSecurePass!456"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_credentials", (await ReadJson(response))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Me_returns_authenticated_user_from_session_cookie()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();
        (await Login(client)).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1/auth/me");
        var payload = await ReadJson(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("operator", payload["data"]?["user"]?["username"]?.GetValue<string>());
        Assert.Equal("operator", payload["data"]?["user"]?["role"]?.GetValue<string>());
    }

    [Fact]
    public async Task Me_rehydrates_the_full_user_from_the_cookie()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();
        (await Login(client)).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1/auth/me");
        var payload = await ReadJson(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("operator", payload["data"]?["user"]?["username"]?.GetValue<string>());
        Assert.Equal("operator", payload["data"]?["user"]?["role"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(payload["data"]?["expiresAt"]?.GetValue<string>()));
    }

    [Fact]
    public async Task Logout_expires_the_cookie_and_removes_access()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();
        (await Login(client)).EnsureSuccessStatusCode();

        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        var cookie = Assert.Single(logout.Headers.GetValues("Set-Cookie"));

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Contains($"{AuthSession.CookieName}=", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    private static TransitOpsApiFactory FactoryWithOperator() => new(db =>
        db.AppUsers.Add(TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator)));

    private static Task<HttpResponseMessage> Login(HttpClient client) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { username = "operator", password = "SecurePass!123" });

    private static async Task<JsonNode> ReadJson(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
}

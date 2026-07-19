using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using TransitOps.Api.Domain;
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
    public async Task Login_returns_token_with_common_response_contract()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();

        var response = await Login(client);
        var payload = await ReadJson(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(payload["data"]?["accessToken"]?.GetValue<string>()));
        Assert.Equal("operator", payload["data"]?["user"]?["role"]?.GetValue<string>());
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
    public async Task Session_returns_unauthorized_contract_without_token()
    {
        using var factory = new TransitOpsApiFactory();
        using var response = await factory.CreateClient().GetAsync("/api/v1/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("authentication_required", (await ReadJson(response))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Admin_check_returns_forbidden_contract_for_operator()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();
        var login = await ReadJson(await Login(client));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            login["data"]?["accessToken"]?.GetValue<string>());

        var response = await client.GetAsync("/api/v1/auth/admin-check");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("authorization_forbidden", (await ReadJson(response))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Session_returns_authenticated_user_from_issued_token()
    {
        using var factory = FactoryWithOperator();
        using var client = factory.CreateClient();
        var login = await ReadJson(await Login(client));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            login["data"]?["accessToken"]?.GetValue<string>());

        var response = await client.GetAsync("/api/v1/auth/session");
        var payload = await ReadJson(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("operator", payload["data"]?["username"]?.GetValue<string>());
        Assert.Equal("operator", payload["data"]?["role"]?.GetValue<string>());
    }

    private static TransitOpsApiFactory FactoryWithOperator() => new(db =>
        db.AppUsers.Add(TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator)));

    private static Task<HttpResponseMessage> Login(HttpClient client) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { username = "operator", password = "SecurePass!123" });

    private static async Task<JsonNode> ReadJson(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
}

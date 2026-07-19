using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using TransitOps.Api.Domain;
using TransitOps.Tests.Support;

namespace TransitOps.Tests.Controllers;

public sealed class CatalogControllerTests
{
    [Theory]
    [InlineData("/api/v1/vehicles")]
    [InlineData("/api/v1/drivers")]
    [InlineData("/api/v1/customers")]
    public async Task Catalogs_require_authentication(string path)
    {
        using var factory = FactoryWithOperator();
        using var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("authentication_required", (await ReadJson(response))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Vehicle_endpoints_complete_crud_with_common_contract_and_conflict_handling()
    {
        using var factory = FactoryWithOperator();
        using var client = await AuthenticatedClient(factory);
        var createdResponse = await client.PostAsJsonAsync("/api/v1/vehicles", new
        {
            licensePlate = "1234 abc", internalCode = "V-1", brand = "Volvo", model = "FH", loadCapacity = 15000
        });
        var created = await ReadJson(createdResponse);
        var id = created["data"]?["id"]?.GetValue<string>();
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal("1234 ABC", created["data"]?["licensePlate"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(created["requestId"]?.GetValue<string>()));

        var list = await ReadJson(await client.GetAsync("/api/v1/vehicles"));
        Assert.Single(list["data"]!.AsArray());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/vehicles/{id}", new
        {
            licensePlate = "1234 ABC", internalCode = "V-1", brand = "MAN", model = "TGX", loadCapacity = 16000
        })).StatusCode);

        var conflictResponse = await client.PostAsJsonAsync("/api/v1/vehicles", new { licensePlate = "1234 ABC" });
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal("vehicle_plate_conflict", (await ReadJson(conflictResponse))["error"]?["code"]?.GetValue<string>());

        var deleted = await client.DeleteAsync($"/api/v1/vehicles/{id}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.True((await ReadJson(deleted))["data"]?["deactivated"]?.GetValue<bool>());
        Assert.Empty((await ReadJson(await client.GetAsync("/api/v1/vehicles")))["data"]!.AsArray());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/vehicles/{id}")).StatusCode);
    }

    [Fact]
    public async Task Catalog_validation_errors_include_field_details()
    {
        using var factory = FactoryWithOperator();
        using var client = await AuthenticatedClient(factory);
        var response = await client.PostAsJsonAsync("/api/v1/drivers", new { name = "", licenseNumber = "" });
        var body = await ReadJson(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", body["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(body["error"]?["details"]?["Name"]);
        Assert.NotNull(body["error"]?["details"]?["LicenseNumber"]);
    }

    [Fact]
    public async Task Driver_and_customer_endpoints_create_list_update_and_deactivate()
    {
        using var factory = FactoryWithOperator();
        using var client = await AuthenticatedClient(factory);
        var driver = await ReadJson(await client.PostAsJsonAsync("/api/v1/drivers", new
            { name = "Ana Pérez", licenseNumber = "B-123", employeeCode = "E-7", contactDetails = "600 000 000" }));
        var customer = await ReadJson(await client.PostAsJsonAsync("/api/v1/customers", new
            { name = "Acme", contactDetails = "info@acme.test" }));
        var driverId = driver["data"]?["id"]?.GetValue<string>();
        var customerId = customer["data"]?["id"]?.GetValue<string>();

        Assert.Single((await ReadJson(await client.GetAsync("/api/v1/drivers")))["data"]!.AsArray());
        Assert.Single((await ReadJson(await client.GetAsync("/api/v1/customers")))["data"]!.AsArray());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/drivers/{driverId}", new
            { name = "Ana P.", licenseNumber = "B-123" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/customers/{customerId}", new
            { name = "Acme Norte" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/v1/drivers/{driverId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/api/v1/customers/{customerId}")).StatusCode);
    }

    private static TransitOpsApiFactory FactoryWithOperator() => new(db =>
        db.AppUsers.Add(TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator)));

    private static async Task<HttpClient> AuthenticatedClient(TransitOpsApiFactory factory)
    {
        var client = factory.CreateClient();
        var login = await ReadJson(await client.PostAsJsonAsync("/api/v1/auth/login", new
            { username = "operator", password = "SecurePass!123" }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login["data"]?["accessToken"]?.GetValue<string>());
        return client;
    }

    private static async Task<JsonNode> ReadJson(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;
using TransitOps.Tests.Support;

namespace TransitOps.Tests.Controllers;

public sealed class ShipmentEventsControllerTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Endpoints_require_authentication(string method)
    {
        using var factory = Factory(); using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/v1/shipments/{Guid.NewGuid()}/events");
        if (method == "POST") request.Content = JsonContent.Create(new { eventType = "checkpoint" });
        using var response = await factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_and_get_use_the_common_contract_and_token_actor()
    {
        var shipment = Shipment("A"); using var factory = Factory(db => db.Shipments.Add(shipment)); using var client = await Client(factory);
        var createdResponse = await client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/events", new { eventType = "incident", occurredAt = DateTime.UtcNow.AddMinutes(-1), location = "Madrid", notes = "Retraso" });
        var created = await Json(createdResponse);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode); Assert.NotNull(created["requestId"]);
        Assert.Equal("operator", created["data"]!["recordedByUsername"]!.GetValue<string>());

        var historyResponse = await client.GetAsync($"/api/v1/shipments/{shipment.Id}/events"); var history = await Json(historyResponse);
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode); Assert.Single(history["data"]!.AsArray());
        Assert.Equal("operator", history["data"]![0]!["recordedByUsername"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("delivered")]
    [InlineData("foo")]
    public async Task Manual_endpoint_rejects_system_and_unknown_types(string eventType)
    {
        var shipment = Shipment("A"); using var factory = Factory(db => db.Shipments.Add(shipment)); using var client = await Client(factory);
        var response = await client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/events", new { eventType }); var json = await Json(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal("validation_error", json["error"]!["code"]!.GetValue<string>());
        Assert.NotNull(json["error"]!["details"]!["EventType"]);
    }

    [Fact]
    public async Task Missing_shipment_history_is_404_instead_of_an_empty_list()
    {
        using var factory = Factory(); using var client = await Client(factory);
        var response = await client.GetAsync($"/api/v1/shipments/{Guid.NewGuid()}/events"); var json = await Json(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); Assert.Equal("shipment_not_found", json["error"]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task History_contains_automatic_creation_assignment_and_departure_events()
    {
        using var factory = Factory(); using var client = await Client(factory);
        var vehicle = await Json(await client.PostAsJsonAsync("/api/v1/vehicles", new { licensePlate = "1234 ABC" })); var vehicleId = vehicle["data"]!["id"]!.GetValue<string>();
        var driver = await Json(await client.PostAsJsonAsync("/api/v1/drivers", new { name = "Ana", licenseNumber = "L-1" })); var driverId = driver["data"]!["id"]!.GetValue<string>();
        var created = await Json(await client.PostAsJsonAsync("/api/v1/shipments", new { reference = "TRACE", origin = "A", destination = "B", plannedPickupAt = DateTime.UtcNow.AddDays(1) })); var shipmentId = created["data"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/shipments/{shipmentId}/assignment", new { vehicleId, driverId })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/shipments/{shipmentId}/status", new { status = "in_progress" })).StatusCode);

        var history = await Json(await client.GetAsync($"/api/v1/shipments/{shipmentId}/events"));
        Assert.Equal(["created", "assigned", "departed"], history["data"]!.AsArray().Select(item => item!["eventType"]!.GetValue<string>()));
    }

    private static Shipment Shipment(string reference) => new() { Reference = reference, Origin = "A", Destination = "B", PlannedPickupAt = DateTime.UtcNow.AddDays(1) };
    private static TransitOpsApiFactory Factory(Action<TransitOpsDbContext>? seed = null) => new(db => { db.AppUsers.Add(TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator)); seed?.Invoke(db); });
    private static async Task<HttpClient> Client(TransitOpsApiFactory factory) { var client = factory.CreateClient(); var login = await Json(await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "operator", password = "SecurePass!123" })); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login["data"]!["accessToken"]!.GetValue<string>()); return client; }
    private static async Task<JsonNode> Json(HttpResponseMessage response) => JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
}

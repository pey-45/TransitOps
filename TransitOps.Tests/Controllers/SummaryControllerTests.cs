using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using TransitOps.Api.Domain;
using TransitOps.Tests.Support;

namespace TransitOps.Tests.Controllers;

public sealed class SummaryControllerTests
{
    [Fact]
    public async Task Summary_requires_authentication()
    {
        using var factory = Factory();
        using var response = await factory.CreateClient().GetAsync("/api/v1/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("authentication_required", (await Json(response))["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Operator_receives_status_activity_and_incident_summary()
    {
        var vehicle = new Vehicle { LicensePlate = "1234 ABC" };
        var driver = new Driver { Name = "Ana", LicenseNumber = "L-1" };
        var pickup = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var shipment = new Shipment
        {
            Reference = "ENV-1",
            Origin = "A",
            Destination = "B",
            PlannedPickupAt = pickup,
            Status = ShipmentStatus.InProgress,
            VehicleId = vehicle.Id,
            DriverId = driver.Id
        };
        using var factory = Factory(db =>
        {
            db.AddRange(vehicle, driver, shipment);
            db.ShipmentEvents.Add(new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                EventType = ShipmentEventType.Incident,
                OccurredAt = pickup
            });
        });
        using var client = await Client(factory);

        var response = await client.GetAsync(
            "/api/v1/summary?from=2026-08-01T00:00:00Z&to=2026-08-01T23:59:59Z");
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body["data"]?["shipments"]?["inProgress"]?.GetValue<int>());
        Assert.Equal("1234 ABC", body["data"]?["vehicles"]?[0]?["label"]?.GetValue<string>());
        Assert.Equal("Ana", body["data"]?["drivers"]?[0]?["label"]?.GetValue<string>());
        Assert.Equal(1, body["data"]?["incidents"]?.GetValue<int>());
    }

    [Fact]
    public async Task Inverted_period_returns_validation_details_for_to()
    {
        using var factory = Factory();
        using var client = await Client(factory);

        var response = await client.GetAsync(
            "/api/v1/summary?from=2026-08-02T00:00:00Z&to=2026-08-01T00:00:00Z");
        var body = await Json(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", body["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(body["error"]?["details"]?["To"]);
    }

    private static TransitOpsApiFactory Factory(Action<TransitOps.Api.Persistence.TransitOpsDbContext>? seed = null) =>
        new(db =>
        {
            db.AppUsers.Add(TransitOpsApiFactory.CreateUser(
                "operator",
                "SecurePass!123",
                UserRole.Operator));
            seed?.Invoke(db);
        });
    private static async Task<HttpClient> Client(TransitOpsApiFactory factory)
    {
        var client = factory.CreateClient();
        (await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username = "operator", password = "SecurePass!123" })).EnsureSuccessStatusCode();
        return client;
    }
    private static async Task<JsonNode> Json(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
}

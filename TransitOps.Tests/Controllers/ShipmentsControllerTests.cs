using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;
using TransitOps.Tests.Support;

namespace TransitOps.Tests.Controllers;

public sealed class ShipmentsControllerTests
{
    [Theory]
    [InlineData("GET", "/api/v1/shipments")]
    [InlineData("POST", "/api/v1/shipments")]
    [InlineData("PUT", "/api/v1/shipments/00000000-0000-0000-0000-000000000001/assignment")]
    [InlineData("DELETE", "/api/v1/shipments/00000000-0000-0000-0000-000000000001/assignment")]
    [InlineData("POST", "/api/v1/shipments/00000000-0000-0000-0000-000000000001/status")]
    public async Task Endpoints_require_authentication(string method, string path)
    {
        using var factory = Factory(); using var request = new HttpRequestMessage(new HttpMethod(method), path); if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
        using var response = await factory.CreateClient().SendAsync(request); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Crud_uses_common_contract_and_documents_conflict_not_found_and_no_delete()
    {
        using var factory = Factory(); using var client = await Client(factory);
        var createdResponse = await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload()); var created = await Json(createdResponse); var id = created["data"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode); Assert.Equal("planned", created["data"]!["status"]!.GetValue<string>()); Assert.NotNull(created["requestId"]);
        Assert.Single((await Json(await client.GetAsync("/api/v1/shipments")))["data"]!["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/shipments/{id}", ValidPayload("REF-1"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload(" ref-1 "))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/shipments/{Guid.NewGuid()}" )).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync($"/api/v1/shipments/{id}")).StatusCode);
    }

    [Fact]
    public async Task Input_dates_are_normalized_to_utc_for_all_json_forms()
    {
        using var factory = Factory(); using var client = await Client(factory);
        foreach (var (reference, date) in new[] { ("Z", "2026-08-01T08:00:00Z"), ("NAIVE", "2026-08-01T08:00:00"), ("OFFSET", "2026-08-01T10:00:00+02:00") })
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload(reference, date))).StatusCode);
        using var scope = factory.Services.CreateScope(); var dates = await scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>().Shipments.Select(item => item.PlannedPickupAt).ToListAsync();
        Assert.All(dates, value => Assert.Equal(DateTimeKind.Utc, value.Kind)); Assert.All(dates, value => Assert.Equal(new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc), value));
    }

    [Fact]
    public async Task Validation_has_field_details_and_rejects_bad_query_values()
    {
        using var factory = Factory(); using var client = await Client(factory);
        var invalid = await Json(await client.PostAsJsonAsync("/api/v1/shipments", new { reference = "", origin = "", destination = "" })); Assert.NotNull(invalid["error"]!["details"]!["Reference"]);
        var dates = await Json(await client.PostAsJsonAsync("/api/v1/shipments", new { reference = "A", origin = "O", destination = "D", plannedPickupAt = "2026-08-02T00:00:00Z", plannedDeliveryAt = "2026-08-01T00:00:00Z" })); Assert.NotNull(dates["error"]!["details"]!["PlannedDeliveryAt"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/shipments?status=foo")).StatusCode); Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/shipments?pageSize=500")).StatusCode);
    }

    [Fact]
    public async Task Filters_and_pagination_are_bound_from_query_string()
    {
        using var factory = Factory(); using var client = await Client(factory); await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload("A", "2026-08-01T08:00:00Z")); await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload("B", "2026-08-01T09:00:00Z"));
        var result = await Json(await client.GetAsync("/api/v1/shipments?status=planned&pickupFrom=2026-08-01&pickupTo=2026-08-01T23:59:59Z&page=1&pageSize=1"));
        Assert.Single(result["data"]!["items"]!.AsArray()); Assert.Equal(2, result["data"]!["totalPages"]!.GetValue<int>());
    }

    [Fact]
    public async Task Inactive_customer_returns_conflict()
    {
        var customer = new Customer { Name = "Old", IsActive = false }; using var factory = Factory(db => db.Customers.Add(customer)); using var client = await Client(factory);
        var response = await client.PostAsJsonAsync("/api/v1/shipments", new { reference = "A", origin = "O", destination = "D", plannedPickupAt = "2026-08-01T08:00:00", customerId = customer.Id });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Equal("shipment_customer_not_found", (await Json(response))["error"]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Complete_operation_flow_assigns_starts_delivers_and_blocks_terminal_changes()
    {
        using var factory = Factory(); using var client = await Client(factory);
        var vehicle = await Json(await client.PostAsJsonAsync("/api/v1/vehicles", new { licensePlate = "1234 ABC", loadCapacity = 10000 })); var vehicleId = vehicle["data"]!["id"]!.GetValue<string>();
        var driver = await Json(await client.PostAsJsonAsync("/api/v1/drivers", new { name = "Ana", licenseNumber = "L-1" })); var driverId = driver["data"]!["id"]!.GetValue<string>();
        var created = await Json(await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload())); var shipmentId = created["data"]!["id"]!.GetValue<string>();
        var assignedResponse = await client.PutAsJsonAsync($"/api/v1/shipments/{shipmentId}/assignment", new { vehicleId, driverId }); var assigned = await Json(assignedResponse);
        Assert.Equal(HttpStatusCode.OK, assignedResponse.StatusCode); Assert.Equal("1234 ABC", assigned["data"]!["vehiclePlate"]!.GetValue<string>()); Assert.Equal("Ana", assigned["data"]!["driverName"]!.GetValue<string>());
        var started = await Json(await client.PostAsJsonAsync($"/api/v1/shipments/{shipmentId}/status", new { status = "in_progress" })); Assert.NotNull(started["data"]!["actualPickupAt"]);
        var delivered = await Json(await client.PostAsJsonAsync($"/api/v1/shipments/{shipmentId}/status", new { status = "delivered" })); Assert.Equal("delivered", delivered["data"]!["status"]!.GetValue<string>()); Assert.NotNull(delivered["data"]!["actualDeliveryAt"]);
        var terminal = await client.PostAsJsonAsync($"/api/v1/shipments/{shipmentId}/status", new { status = "cancelled" }); Assert.Equal(HttpStatusCode.Conflict, terminal.StatusCode); Assert.Equal("shipment_status_terminal", (await Json(terminal))["error"]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Operation_bodies_use_the_common_validation_contract()
    {
        var shipment = new Shipment { Reference = "A", Origin = "O", Destination = "D", PlannedPickupAt = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc) }; using var factory = Factory(db => db.Shipments.Add(shipment)); using var client = await Client(factory);
        foreach (var body in new object[] { new { status = "foo" }, new { } })
        {
            var response = await client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/status", body); var json = await Json(response); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal("validation_error", json["error"]!["code"]!.GetValue<string>()); Assert.NotNull(json["error"]!["details"]!["Status"]);
        }
        var malformed = await client.PutAsJsonAsync($"/api/v1/shipments/{shipment.Id}/assignment", new { vehicleId = "not-a-guid", driverId = Guid.NewGuid() }); Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }

    [Fact]
    public async Task Assignment_rejects_a_resource_occupied_by_another_open_shipment()
    {
        var vehicle = new Vehicle { LicensePlate = "BUSY" }; var driver = new Driver { Name = "Ana", LicenseNumber = "L" }; using var factory = Factory(db => db.AddRange(vehicle, driver)); using var client = await Client(factory);
        var first = await Json(await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload("OWNER"))); var firstId = first["data"]!["id"]!.GetValue<string>();
        var second = await Json(await client.PostAsJsonAsync("/api/v1/shipments", ValidPayload("TARGET"))); var secondId = second["data"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/shipments/{firstId}/assignment", new { vehicleId = vehicle.Id, driverId = driver.Id })).StatusCode);
        var conflict = await client.PutAsJsonAsync($"/api/v1/shipments/{secondId}/assignment", new { vehicleId = vehicle.Id, driverId = driver.Id }); var json = await Json(conflict);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); Assert.Equal("shipment_vehicle_busy", json["error"]!["code"]!.GetValue<string>()); Assert.Contains("OWNER", json["error"]!["message"]!.GetValue<string>());
    }

    private static object ValidPayload(string reference = "REF-1", string date = "2026-08-01T08:00:00Z") => new { reference, origin = "Madrid", destination = "Barcelona", plannedPickupAt = date };
    private static TransitOpsApiFactory Factory(Action<TransitOpsDbContext>? seed = null) => new(db => { db.AppUsers.Add(TransitOpsApiFactory.CreateUser("operator", "SecurePass!123", UserRole.Operator)); seed?.Invoke(db); });
    private static async Task<HttpClient> Client(TransitOpsApiFactory factory) { var client = factory.CreateClient(); (await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "operator", password = "SecurePass!123" })).EnsureSuccessStatusCode(); return client; }
    private static async Task<JsonNode> Json(HttpResponseMessage response) => JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
}

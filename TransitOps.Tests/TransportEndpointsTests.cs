using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Tests;

public sealed class TransportEndpointsTests
{
    [Fact]
    public async Task Create_ReturnsCreatedAndPersistsTransport_WhenRequestIsValid()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/transports",
            CreateTransportRequest(reference: "TR-200"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await ReadJsonAsync(response);
        var transportId = payload["data"]?["id"]?.GetValue<string>();

        Assert.NotNull(transportId);
        Assert.Equal("TR-200", payload["data"]?["reference"]?.GetValue<string>());
        Assert.Equal("Madrid", payload["data"]?["origin"]?.GetValue<string>());
        Assert.Equal("Barcelona", payload["data"]?["destination"]?.GetValue<string>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var transport = await dbContext.Transports.SingleAsync();

        Assert.Equal(Guid.Parse(transportId!), transport.Id);
        Assert.Equal(TransportStatus.Planned, transport.Status);
        Assert.Null(transport.VehicleId);
        Assert.Null(transport.DriverId);
        Assert.Equal("High-priority route", transport.Description);
        Assert.Equal(DateTimeKind.Unspecified, transport.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, transport.UpdatedAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, transport.PlannedPickupAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, transport.PlannedDeliveryAt!.Value.Kind);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenReferenceAlreadyExistsOnActiveTransport()
    {
        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(CreateTransport(
                reference: "TR-201",
                plannedPickupAt: new DateTime(2026, 3, 29, 8, 0, 0, DateTimeKind.Utc)));
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/transports",
            CreateTransportRequest(reference: "TR-201"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("transport_reference_conflict", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenPlannedDeliveryPrecedesPickup()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/transports",
            CreateTransportRequest(
                reference: "TR-202",
                plannedPickupAt: new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
                plannedDeliveryAt: new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("validation_error", payload["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(payload["error"]?["details"]?["PlannedDeliveryAt"]);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenPlannedPickupIsMissing()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/transports",
            CreateTransportRequest(
                reference: "TR-202A",
                plannedPickupAt: DateTime.MinValue));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("validation_error", payload["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(payload["error"]?["details"]?["PlannedPickupAt"]);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveTransports_OrderedByPickupDateThenReference()
    {
        var firstTransport = CreateTransport(
            reference: "TR-001",
            plannedPickupAt: new DateTime(2026, 3, 29, 8, 0, 0, DateTimeKind.Utc));
        var secondTransport = CreateTransport(
            reference: "TR-002",
            plannedPickupAt: new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc));
        var deletedTransport = CreateTransport(
            reference: "TR-003",
            plannedPickupAt: new DateTime(2026, 3, 29, 6, 0, 0, DateTimeKind.Utc),
            deletedAt: new DateTime(2026, 3, 29, 7, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.AddRange(firstTransport, secondTransport, deletedTransport);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/transports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var transports = payload["data"]!.AsArray();

        Assert.Equal(2, transports.Count);
        Assert.Equal(firstTransport.Reference, transports[0]?["reference"]?.GetValue<string>());
        Assert.Equal(secondTransport.Reference, transports[1]?["reference"]?.GetValue<string>());
        Assert.DoesNotContain(transports, node => node?["reference"]?.GetValue<string>() == deletedTransport.Reference);
    }

    [Fact]
    public async Task Update_ReturnsUpdatedTransport_WhenTransportExists()
    {
        var transport = CreateTransport(
            reference: "TR-300",
            plannedPickupAt: new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc),
            status: TransportStatus.InTransit);

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(transport);
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/transports/{transport.Id}",
            CreateTransportRequest(
                reference: "TR-300-UPDATED",
                origin: "Seville",
                destination: "Valencia",
                description: "Updated route",
                plannedPickupAt: new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc),
                plannedDeliveryAt: new DateTime(2026, 4, 2, 18, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("TR-300-UPDATED", payload["data"]?["reference"]?.GetValue<string>());
        Assert.Equal("Seville", payload["data"]?["origin"]?.GetValue<string>());
        Assert.Equal("Valencia", payload["data"]?["destination"]?.GetValue<string>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var storedTransport = await dbContext.Transports.SingleAsync();

        Assert.Equal("TR-300-UPDATED", storedTransport.Reference);
        Assert.Equal("Updated route", storedTransport.Description);
        Assert.Equal(TransportStatus.InTransit, storedTransport.Status);
        Assert.Equal(DateTimeKind.Unspecified, storedTransport.PlannedPickupAt.Kind);
        Assert.Equal(DateTimeKind.Unspecified, storedTransport.PlannedDeliveryAt!.Value.Kind);
        Assert.Equal(DateTimeKind.Unspecified, storedTransport.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenTransportDoesNotExist()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/transports/{Guid.NewGuid()}",
            CreateTransportRequest(reference: "TR-301"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("transport_not_found", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Update_ReturnsConflict_WhenReferenceAlreadyBelongsToAnotherActiveTransport()
    {
        var existingTransport = CreateTransport(
            reference: "TR-302",
            plannedPickupAt: new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc));
        var targetTransport = CreateTransport(
            reference: "TR-303",
            plannedPickupAt: new DateTime(2026, 4, 3, 9, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.AddRange(existingTransport, targetTransport);
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/transports/{targetTransport.Id}",
            CreateTransportRequest(reference: "TR-302"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("transport_reference_conflict", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetById_ReturnsTransportDetail_WhenTransportExists()
    {
        var transport = CreateTransport(
            reference: "TR-100",
            plannedPickupAt: new DateTime(2026, 3, 30, 8, 0, 0, DateTimeKind.Utc),
            description: "High-priority route");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(transport);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/transports/{transport.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal(transport.Id.ToString(), payload["data"]?["id"]?.GetValue<string>());
        Assert.Equal(transport.Reference, payload["data"]?["reference"]?.GetValue<string>());
        Assert.Equal(transport.Description, payload["data"]?["description"]?.GetValue<string>());
        Assert.Equal(transport.Origin, payload["data"]?["origin"]?.GetValue<string>());
        Assert.Equal(transport.Destination, payload["data"]?["destination"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenTransportDoesNotExist()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/transports/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("transport_not_found", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenTransportIsSoftDeleted()
    {
        var deletedTransport = CreateTransport(
            reference: "TR-404",
            plannedPickupAt: new DateTime(2026, 3, 31, 8, 0, 0, DateTimeKind.Utc),
            deletedAt: new DateTime(2026, 3, 31, 12, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(deletedTransport);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/transports/{deletedTransport.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("transport_not_found", payload["error"]?["code"]?.GetValue<string>());
    }

    private static Transport CreateTransport(
        string reference,
        DateTime plannedPickupAt,
        string? description = null,
        DateTime? deletedAt = null,
        TransportStatus status = TransportStatus.Planned)
    {
        return new Transport(status)
        {
            Reference = reference,
            Description = description,
            Origin = "Madrid",
            Destination = "Barcelona",
            PlannedPickupAt = plannedPickupAt,
            PlannedDeliveryAt = plannedPickupAt.AddHours(6),
            CreatedAt = plannedPickupAt.AddDays(-1),
            UpdatedAt = plannedPickupAt.AddHours(-1),
            DeletedAt = deletedAt
        };
    }

    private static object CreateTransportRequest(
        string reference,
        string origin = "Madrid",
        string destination = "Barcelona",
        string? description = "High-priority route",
        DateTime? plannedPickupAt = null,
        DateTime? plannedDeliveryAt = null)
    {
        var pickupAt = plannedPickupAt ?? new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
        var deliveryAt = plannedDeliveryAt ?? pickupAt.AddHours(6);

        return new
        {
            reference,
            description,
            origin,
            destination,
            plannedPickupAt = pickupAt,
            plannedDeliveryAt = deliveryAt
        };
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}

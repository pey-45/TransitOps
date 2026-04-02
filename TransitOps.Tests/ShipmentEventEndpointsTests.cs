using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Tests;

public sealed class ShipmentEventEndpointsTests
{
    [Fact]
    public async Task GetByTransportId_ReturnsChronologicalHistory_WithActorTraceability()
    {
        var transport = CreateTransport("TR-EVT-001");
        var adminUser = CreateAppUser("seed.admin", "seed.admin@transitops.dev");
        var operatorUser = CreateAppUser("seed.operator", "seed.operator@transitops.dev", role: UserRole.Operator);
        var laterEvent = CreateShipmentEvent(
            transport.Id,
            operatorUser.Id,
            ShipmentEventType.Assigned,
            new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc),
            location: "Madrid");
        var earlierEvent = CreateShipmentEvent(
            transport.Id,
            adminUser.Id,
            ShipmentEventType.Created,
            new DateTime(2026, 4, 3, 9, 0, 0, DateTimeKind.Utc),
            location: "Madrid");
        var deletedEvent = CreateShipmentEvent(
            transport.Id,
            adminUser.Id,
            ShipmentEventType.Incident,
            new DateTime(2026, 4, 3, 11, 0, 0, DateTimeKind.Utc),
            deletedAt: new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc));

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(transport);
            dbContext.AppUsers.AddRange(adminUser, operatorUser);
            dbContext.ShipmentEvents.AddRange(laterEvent, earlierEvent, deletedEvent);
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/transports/{transport.Id}/shipment-events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var shipmentEvents = payload["data"]!.AsArray();

        Assert.Equal(2, shipmentEvents.Count);
        Assert.Equal(0, shipmentEvents[0]?["eventType"]?.GetValue<int>());
        Assert.Equal("seed.admin", shipmentEvents[0]?["createdBy"]?["username"]?.GetValue<string>());
        Assert.Equal("seed.admin@transitops.dev", shipmentEvents[0]?["createdBy"]?["email"]?.GetValue<string>());
        Assert.Equal(1, shipmentEvents[1]?["eventType"]?.GetValue<int>());
        Assert.Equal("seed.operator", shipmentEvents[1]?["createdBy"]?["username"]?.GetValue<string>());
        Assert.Equal("2026-04-03T09:00:00Z", shipmentEvents[0]?["eventDate"]?.GetValue<string>());
        Assert.Equal("2026-04-03T10:00:00Z", shipmentEvents[1]?["eventDate"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetByTransportId_ReturnsNotFound_WhenTransportIsMissing()
    {
        using var factory = new TransitOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/transports/{Guid.NewGuid()}/shipment-events");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("transport_not_found", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsCreatedAndPersistsShipmentEvent_WhenRequestIsValid()
    {
        var transport = CreateTransport("TR-EVT-002");
        var actor = CreateAppUser("seed.admin", "seed.admin@transitops.dev");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(transport);
            dbContext.AppUsers.Add(actor);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/transports/{transport.Id}/shipment-events",
            CreateShipmentEventRequest(actor.Id, "checkpoint", new DateTime(2026, 4, 3, 13, 30, 0, DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await ReadJsonAsync(response);
        var shipmentEventId = payload["data"]?["id"]?.GetValue<string>();

        Assert.NotNull(shipmentEventId);
        Assert.Equal(transport.Id.ToString(), payload["data"]?["transportId"]?.GetValue<string>());
        Assert.Equal(actor.Id.ToString(), payload["data"]?["createdByUserId"]?.GetValue<string>());
        Assert.Equal("seed.admin", payload["data"]?["createdBy"]?["username"]?.GetValue<string>());
        Assert.Equal(3, payload["data"]?["eventType"]?.GetValue<int>());
        Assert.Equal("2026-04-03T13:30:00", payload["data"]?["eventDate"]?.GetValue<string>());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
        var shipmentEvent = await dbContext.ShipmentEvents.SingleAsync();

        Assert.Equal(Guid.Parse(shipmentEventId!), shipmentEvent.Id);
        Assert.Equal(transport.Id, shipmentEvent.TransportId);
        Assert.Equal(actor.Id, shipmentEvent.CreatedByUserId);
        Assert.Equal(ShipmentEventType.Checkpoint, shipmentEvent.EventType);
        Assert.Equal(DateTimeKind.Unspecified, shipmentEvent.EventDate.Kind);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenTransportIsMissing()
    {
        var actor = CreateAppUser("seed.admin", "seed.admin@transitops.dev");

        using var factory = new TransitOpsApiFactory(dbContext => dbContext.AppUsers.Add(actor));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/transports/{Guid.NewGuid()}/shipment-events",
            CreateShipmentEventRequest(actor.Id, "created", new DateTime(2026, 4, 3, 9, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("transport_not_found", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenActorDoesNotExist()
    {
        var transport = CreateTransport("TR-EVT-003");

        using var factory = new TransitOpsApiFactory(dbContext => dbContext.Transports.Add(transport));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/transports/{transport.Id}/shipment-events",
            CreateShipmentEventRequest(Guid.NewGuid(), "created", new DateTime(2026, 4, 3, 9, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("shipment_event_actor_not_found", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenActorIsInactive()
    {
        var transport = CreateTransport("TR-EVT-004");
        var actor = CreateAppUser("seed.operator", "seed.operator@transitops.dev", role: UserRole.Operator, isActive: false);

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(transport);
            dbContext.AppUsers.Add(actor);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/transports/{transport.Id}/shipment-events",
            CreateShipmentEventRequest(actor.Id, "incident", new DateTime(2026, 4, 3, 15, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("shipment_event_actor_inactive", payload["error"]?["code"]?.GetValue<string>());
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenEventTypeIsInvalid()
    {
        var transport = CreateTransport("TR-EVT-005");
        var actor = CreateAppUser("seed.admin", "seed.admin@transitops.dev");

        using var factory = new TransitOpsApiFactory(dbContext =>
        {
            dbContext.Transports.Add(transport);
            dbContext.AppUsers.Add(actor);
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/transports/{transport.Id}/shipment-events",
            CreateShipmentEventRequest(actor.Id, "teleported", new DateTime(2026, 4, 3, 16, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("validation_error", payload["error"]?["code"]?.GetValue<string>());
        Assert.NotNull(payload["error"]?["details"]?["EventType"]);
    }

    private static Transport CreateTransport(string reference, DateTime? deletedAt = null)
    {
        var plannedPickupAt = new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc);

        return new Transport
        {
            Reference = reference,
            Description = "Shipment event test transport",
            Origin = "Madrid",
            Destination = "Barcelona",
            PlannedPickupAt = plannedPickupAt,
            PlannedDeliveryAt = plannedPickupAt.AddHours(6),
            CreatedAt = plannedPickupAt.AddDays(-1),
            UpdatedAt = plannedPickupAt.AddHours(-1),
            DeletedAt = deletedAt
        };
    }

    private static AppUser CreateAppUser(
        string username,
        string email,
        UserRole role = UserRole.Admin,
        bool isActive = true,
        DateTime? deletedAt = null)
    {
        return new AppUser
        {
            Username = username,
            Email = email,
            PasswordHash = "hashed-password",
            UserRole = role,
            IsActive = isActive,
            CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
            DeletedAt = deletedAt
        };
    }

    private static ShipmentEvent CreateShipmentEvent(
        Guid transportId,
        Guid createdByUserId,
        ShipmentEventType eventType,
        DateTime eventDate,
        string? location = null,
        DateTime? deletedAt = null)
    {
        return new ShipmentEvent
        {
            TransportId = transportId,
            CreatedByUserId = createdByUserId,
            EventType = eventType,
            EventDate = eventDate,
            Location = location,
            Notes = "Seeded shipment event",
            CreatedAt = eventDate,
            DeletedAt = deletedAt
        };
    }

    private static object CreateShipmentEventRequest(Guid createdByUserId, string eventType, DateTime eventDate)
    {
        return new
        {
            createdByUserId,
            eventType,
            eventDate,
            location = "Checkpoint A-4",
            notes = "Shipment event created through integration test."
        };
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}

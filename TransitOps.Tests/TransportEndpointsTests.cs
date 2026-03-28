using System.Net;
using System.Text.Json.Nodes;
using TransitOps.Api.Domain.Entities;

namespace TransitOps.Tests;

public sealed class TransportEndpointsTests
{
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
        DateTime? deletedAt = null)
    {
        return new Transport
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

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!;
    }
}

using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Shipments;
using TransitOps.Api.Persistence;
using TransitOps.Api.Security;

namespace TransitOps.Tests.Services;

public sealed class ShipmentEventServiceTests
{
    [Fact]
    public async Task Create_normalizes_fields_dates_and_records_the_current_user()
    {
        await using var db = CreateDatabase(); var shipment = Shipment("A"); var user = User("operator"); db.AddRange(shipment, user); await db.SaveChangesAsync();
        var service = Service(db, user.Id);
        var unspecified = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-20), DateTimeKind.Unspecified);
        var first = await service.CreateAsync(shipment.Id, new("checkpoint", unspecified, "  Madrid  ", "  Control correcto  "), default);
        var local = DateTime.Now.AddMinutes(-10);
        var second = await service.CreateAsync(shipment.Id, new("incident", local, null, null), default);

        Assert.Equal(DateTimeKind.Utc, first.OccurredAt.Kind); Assert.Equal(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc), first.OccurredAt);
        Assert.Equal(local.ToUniversalTime(), second.OccurredAt); Assert.Equal(user.Id, first.RecordedByUserId);
        Assert.Equal("operator", first.RecordedByUsername); Assert.Equal("Madrid", first.Location); Assert.Equal("Control correcto", first.Notes);
    }

    [Fact]
    public async Task Missing_occurred_at_uses_utc_now()
    {
        await using var db = CreateDatabase(); var shipment = Shipment("A"); db.Shipments.Add(shipment); await db.SaveChangesAsync(); var before = DateTime.UtcNow;
        var result = await Service(db).CreateAsync(shipment.Id, new("checkpoint", null, null, null), default); var after = DateTime.UtcNow;
        Assert.InRange(result.OccurredAt, before, after); Assert.Equal(DateTimeKind.Utc, result.OccurredAt.Kind);
    }

    [Fact]
    public async Task Future_date_is_rejected_beyond_tolerance_and_allowed_within_it()
    {
        await using var db = CreateDatabase(); var shipment = Shipment("A"); db.Shipments.Add(shipment); await db.SaveChangesAsync(); var service = Service(db);
        var error = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(shipment.Id, new("incident", DateTime.UtcNow.AddMinutes(5), null, null), default));
        Assert.Equal(400, error.StatusCode); Assert.Equal("shipment_event_future", error.Code);
        var accepted = await service.CreateAsync(shipment.Id, new("incident", DateTime.UtcNow.AddMinutes(1), null, null), default);
        Assert.Equal("incident", accepted.EventType);
    }

    [Fact]
    public async Task Missing_shipment_is_404_for_creation_and_history()
    {
        await using var db = CreateDatabase(); var service = Service(db); var id = Guid.NewGuid();
        foreach (var operation in new Func<Task>[]
        {
            () => service.CreateAsync(id, new("checkpoint", null, null, null), default),
            () => service.GetByShipmentAsync(id, default)
        })
        {
            var error = await Assert.ThrowsAsync<ApiException>(operation);
            Assert.Equal(404, error.StatusCode); Assert.Equal("shipment_not_found", error.Code);
        }
    }

    [Fact]
    public async Task History_has_a_total_stable_chronological_order()
    {
        await using var db = CreateDatabase(); var shipment = Shipment("A"); var occurredAt = DateTime.UtcNow.AddHours(-1);
        var laterCreated = Event(shipment.Id, ShipmentEventType.Incident, occurredAt, occurredAt.AddMinutes(2));
        var earlierCreated = Event(shipment.Id, ShipmentEventType.Checkpoint, occurredAt, occurredAt.AddMinutes(1));
        var earliest = Event(shipment.Id, ShipmentEventType.Created, occurredAt.AddHours(-1), occurredAt.AddHours(-1));
        db.AddRange(shipment, laterCreated, earliest, earlierCreated); await db.SaveChangesAsync(); var service = Service(db);
        var first = await service.GetByShipmentAsync(shipment.Id, default); var second = await service.GetByShipmentAsync(shipment.Id, default);
        Assert.Equal([earliest.Id, earlierCreated.Id, laterCreated.Id], first.Select(item => item.Id));
        Assert.Equal(first.Select(item => item.Id), second.Select(item => item.Id));
    }

    [Fact]
    public async Task History_resolves_username_and_keeps_system_actor_null()
    {
        await using var db = CreateDatabase(); var shipment = Shipment("A"); var user = User("ana");
        db.AddRange(shipment, user); db.ShipmentEvents.AddRange(
            Event(shipment.Id, ShipmentEventType.Created, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(-2)),
            Event(shipment.Id, ShipmentEventType.Incident, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(-1), user.Id));
        await db.SaveChangesAsync();
        var result = await Service(db).GetByShipmentAsync(shipment.Id, default);
        Assert.Null(result[0].RecordedByUserId); Assert.Null(result[0].RecordedByUsername);
        Assert.Equal(user.Id, result[1].RecordedByUserId); Assert.Equal("ana", result[1].RecordedByUsername);
    }

    [Fact]
    public async Task History_is_isolated_by_shipment()
    {
        await using var db = CreateDatabase(); var first = Shipment("A"); var second = Shipment("B");
        db.AddRange(first, second, Event(first.Id, ShipmentEventType.Created, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(-2)), Event(second.Id, ShipmentEventType.Created, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(-1)));
        await db.SaveChangesAsync();
        var result = await Service(db).GetByShipmentAsync(first.Id, default);
        Assert.Single(result); Assert.Equal(first.Id, result[0].ShipmentId);
    }

    [Fact]
    public async Task Creating_event_does_not_change_shipment_updated_at()
    {
        await using var db = CreateDatabase(); var shipment = Shipment("A"); shipment.UpdatedAt = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc); db.Shipments.Add(shipment); await db.SaveChangesAsync(); var updatedAt = shipment.UpdatedAt;
        await Service(db).CreateAsync(shipment.Id, new("checkpoint", null, null, null), default);
        Assert.Equal(updatedAt, (await db.Shipments.SingleAsync()).UpdatedAt);
    }

    private static ShipmentEventService Service(TransitOpsDbContext db, Guid? userId = null) => new(db, new StubCurrentUser(userId));
    private static Shipment Shipment(string reference) => new() { Reference = reference, Origin = "A", Destination = "B", PlannedPickupAt = DateTime.UtcNow.AddDays(1) };
    private static AppUser User(string username) => new() { Username = username, Email = $"{username}@test.local", PasswordHash = "hash", Role = UserRole.Operator };
    private static ShipmentEvent Event(Guid shipmentId, ShipmentEventType type, DateTime occurredAt, DateTime createdAt, Guid? userId = null) => new() { ShipmentId = shipmentId, EventType = type, OccurredAt = occurredAt, CreatedAt = createdAt, RecordedByUserId = userId };
    private static TransitOpsDbContext CreateDatabase() => new(new DbContextOptionsBuilder<TransitOpsDbContext>().UseInMemoryDatabase($"shipment-event-tests-{Guid.NewGuid():N}").Options);

    private sealed record StubCurrentUser(Guid? Id) : ICurrentUser;
}

using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Reporting;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Services;

public sealed class SummaryServiceTests
{
    [Fact]
    public async Task Status_counts_include_zeroes_and_ignore_the_requested_period()
    {
        await using var db = CreateDatabase();
        db.Shipments.AddRange(
            Shipment("PLANNED", Utc(1), ShipmentStatus.Planned),
            Shipment("DELIVERED-1", Utc(2), ShipmentStatus.Delivered),
            Shipment("DELIVERED-2", Utc(20), ShipmentStatus.Delivered));
        await db.SaveChangesAsync();

        var result = await new SummaryService(db).GetAsync(new(Utc(10), Utc(11)), default);

        Assert.Equal(1, result.Shipments.Planned);
        Assert.Equal(0, result.Shipments.InProgress);
        Assert.Equal(2, result.Shipments.Delivered);
        Assert.Equal(0, result.Shipments.Cancelled);
        Assert.Equal(3, result.Shipments.Total);
    }

    [Fact]
    public async Task Resource_activity_uses_inclusive_planned_pickup_limits_and_excludes_unassigned_shipments()
    {
        await using var db = CreateDatabase();
        var vehicle = new Vehicle { LicensePlate = "1234 ABC" };
        var driver = new Driver { Name = "Ana", LicenseNumber = "L-1" };
        var atStart = Shipment("START", Utc(10), ShipmentStatus.Planned, vehicle.Id, driver.Id);
        var atEnd = Shipment("END", Utc(12), ShipmentStatus.InProgress, vehicle.Id, driver.Id);
        var outside = Shipment("OUTSIDE", Utc(13), ShipmentStatus.Delivered, vehicle.Id, driver.Id);
        var unassigned = Shipment("UNASSIGNED", Utc(11), ShipmentStatus.Planned);
        db.AddRange(vehicle, driver, atStart, atEnd, outside, unassigned);
        await db.SaveChangesAsync();

        var result = await new SummaryService(db).GetAsync(new(Utc(10), Utc(12)), default);

        Assert.Equal(2, Assert.Single(result.Vehicles).ShipmentCount);
        Assert.Equal("1234 ABC", result.Vehicles[0].Label);
        Assert.Equal(2, Assert.Single(result.Drivers).ShipmentCount);
        Assert.Equal("Ana", result.Drivers[0].Label);
    }

    [Fact]
    public async Task Inactive_resources_keep_their_period_activity()
    {
        await using var db = CreateDatabase();
        var vehicle = new Vehicle { LicensePlate = "OLD", IsActive = false };
        var driver = new Driver { Name = "Retirado", LicenseNumber = "OLD", IsActive = false };
        db.AddRange(
            vehicle,
            driver,
            Shipment("HISTORY", Utc(11), ShipmentStatus.Delivered, vehicle.Id, driver.Id));
        await db.SaveChangesAsync();

        var result = await new SummaryService(db).GetAsync(new(Utc(10), Utc(12)), default);

        Assert.Equal("OLD", Assert.Single(result.Vehicles).Label);
        Assert.Equal("Retirado", Assert.Single(result.Drivers).Label);
    }

    [Fact]
    public async Task Incidents_are_counted_by_occurrence_time_and_not_by_creation_or_other_type()
    {
        await using var db = CreateDatabase();
        var shipment = Shipment("A", Utc(10), ShipmentStatus.Planned);
        db.Add(shipment);
        db.ShipmentEvents.AddRange(
            Event(shipment.Id, ShipmentEventType.Incident, Utc(11), Utc(20)),
            Event(shipment.Id, ShipmentEventType.Checkpoint, Utc(11), Utc(11)),
            Event(shipment.Id, ShipmentEventType.Incident, Utc(9), Utc(11)));
        await db.SaveChangesAsync();

        var result = await new SummaryService(db).GetAsync(new(Utc(10), Utc(12)), default);

        Assert.Equal(1, result.Incidents);
    }

    [Fact]
    public async Task Inverted_range_is_rejected_and_date_kinds_are_normalized()
    {
        await using var db = CreateDatabase();
        var service = new SummaryService(db);
        var error = await Assert.ThrowsAsync<ApiException>(() => service.GetAsync(new(Utc(12), Utc(10)), default));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("summary_period_invalid", error.Code);

        var unspecified = DateTime.SpecifyKind(Utc(10), DateTimeKind.Unspecified);
        var result = await service.GetAsync(new(unspecified, unspecified), default);
        Assert.Equal(DateTimeKind.Utc, result.From!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, result.To!.Value.Kind);
    }

    [Fact]
    public async Task Missing_period_defaults_to_the_last_thirty_days()
    {
        await using var db = CreateDatabase();
        var before = DateTime.UtcNow;
        var result = await new SummaryService(db).GetAsync(new(null, null), default);
        var after = DateTime.UtcNow;

        Assert.InRange(result.To!.Value, before, after);
        Assert.Equal(TimeSpan.FromDays(30), result.To.Value - result.From!.Value);
    }

    private static Shipment Shipment(
        string reference,
        DateTime pickup,
        ShipmentStatus status,
        Guid? vehicleId = null,
        Guid? driverId = null) => new()
        {
            Reference = reference,
            Origin = "A",
            Destination = "B",
            PlannedPickupAt = pickup,
            Status = status,
            VehicleId = vehicleId,
            DriverId = driverId
        };
    private static ShipmentEvent Event(
        Guid shipmentId,
        ShipmentEventType eventType,
        DateTime occurredAt,
        DateTime createdAt) => new()
        {
            ShipmentId = shipmentId,
            EventType = eventType,
            OccurredAt = occurredAt,
            CreatedAt = createdAt
        };
    private static DateTime Utc(int day) => new(2026, 8, day, 10, 0, 0, DateTimeKind.Utc);
    private static TransitOpsDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<TransitOpsDbContext>()
            .UseInMemoryDatabase($"summary-tests-{Guid.NewGuid():N}")
            .Options);
}

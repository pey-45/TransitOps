using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Shipments;
using TransitOps.Api.Persistence;

namespace TransitOps.Tests.Services;

public sealed class ShipmentServiceTests
{
    [Fact]
    public async Task Create_normalizes_fields_dates_and_starts_planned()
    {
        await using var db = CreateDatabase(); var service = new ShipmentService(db);
        var local = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Local);
        var result = await service.CreateAsync(Request(" ref-1 ", DateTime.SpecifyKind(new DateTime(2026, 8, 1, 8, 0, 0), DateTimeKind.Unspecified), local), default);
        Assert.Equal("REF-1", result.Reference); Assert.Equal("Madrid", result.Origin); Assert.Equal("planned", result.Status);
        Assert.Equal(DateTimeKind.Utc, result.PlannedPickupAt.Kind); Assert.Equal(DateTimeKind.Utc, result.PlannedDeliveryAt!.Value.Kind);
        Assert.Equal(local.ToUniversalTime(), result.PlannedDeliveryAt);
    }

    [Fact]
    public async Task Create_rejects_duplicate_reference_ignoring_case_and_spaces()
    {
        await using var db = CreateDatabase(); var service = new ShipmentService(db);
        await service.CreateAsync(Request("REF-1"), default);
        var error = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(Request(" ref-1 "), default));
        Assert.Equal(409, error.StatusCode); Assert.Equal("shipment_reference_conflict", error.Code);
    }

    [Fact]
    public async Task Date_rule_rejects_earlier_delivery_but_allows_equal_or_null()
    {
        await using var db = CreateDatabase(); var service = new ShipmentService(db); var pickup = Utc(10);
        var error = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(Request("A", pickup, Utc(9)), default));
        Assert.Equal("shipment_dates_invalid", error.Code);
        await service.CreateAsync(Request("B", pickup, pickup), default); await service.CreateAsync(Request("C", pickup, null), default);
    }

    [Fact]
    public async Task Customer_must_be_active_when_newly_assigned()
    {
        await using var db = CreateDatabase(); var active = new Customer { Name = "Active" }; var inactive = new Customer { Name = "Inactive", IsActive = false };
        db.Customers.AddRange(active, inactive); await db.SaveChangesAsync(); var service = new ShipmentService(db);
        Assert.Equal(active.Id, (await service.CreateAsync(Request("A") with { CustomerId = active.Id }, default)).CustomerId);
        foreach (var id in new[] { inactive.Id, Guid.NewGuid() }) { var error = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(Request(Guid.NewGuid().ToString()) with { CustomerId = id }, default)); Assert.Equal("shipment_customer_not_found", error.Code); }
    }

    [Fact]
    public async Task Update_excludes_itself_preserves_lifecycle_and_accepts_existing_inactive_customer()
    {
        await using var db = CreateDatabase(); var customer = new Customer { Name = "Acme" }; db.Customers.Add(customer); await db.SaveChangesAsync();
        var service = new ShipmentService(db); var created = await service.CreateAsync(Request("A") with { CustomerId = customer.Id }, default);
        var entity = await db.Shipments.SingleAsync(); entity.Status = ShipmentStatus.InProgress; var createdAt = entity.CreatedAt; customer.IsActive = false; await db.SaveChangesAsync();
        var updated = await service.UpdateAsync(created.Id, Request("A") with { CustomerId = customer.Id, Notes = " changed " }, default);
        Assert.Equal("in_progress", updated.Status); Assert.Equal(createdAt, updated.CreatedAt); Assert.Equal("changed", updated.Notes);
        var replacement = new Customer { Name = "Replacement" }; db.Customers.Add(replacement); await db.SaveChangesAsync();
        var reassigned = await service.UpdateAsync(created.Id, Request("A") with { CustomerId = replacement.Id }, default); Assert.Equal("Replacement", reassigned.CustomerName);
        var other = new Customer { Name = "Other", IsActive = false }; db.Customers.Add(other); await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<ApiException>(() => service.UpdateAsync(created.Id, Request("A") with { CustomerId = other.Id }, default)); Assert.Equal("shipment_customer_not_found", error.Code);
    }

    [Fact]
    public async Task Missing_update_is_404_and_missing_get_is_null()
    {
        await using var db = CreateDatabase(); var service = new ShipmentService(db); var id = Guid.NewGuid();
        Assert.Null(await service.GetByIdAsync(id, default)); var error = await Assert.ThrowsAsync<ApiException>(() => service.UpdateAsync(id, Request("A"), default)); Assert.Equal(404, error.StatusCode);
    }

    [Fact]
    public async Task List_applies_each_relational_filter()
    {
        await using var db = CreateDatabase(); var customer = new Customer { Name = "Acme" }; var vehicle = new Vehicle { LicensePlate = "A" }; var driver = new Driver { Name = "Ana", LicenseNumber = "L" };
        db.AddRange(customer, vehicle, driver); var matching = Entity("MATCH", Utc(10)); matching.Status = ShipmentStatus.Delivered; matching.CustomerId = customer.Id; matching.VehicleId = vehicle.Id; matching.DriverId = driver.Id;
        db.Shipments.AddRange(matching, Entity("OTHER", Utc(12))); await db.SaveChangesAsync(); var service = new ShipmentService(db);
        Assert.Single((await service.GetAllAsync(Query(status: "delivered"), default)).Items);
        Assert.Single((await service.GetAllAsync(Query(customerId: customer.Id), default)).Items);
        Assert.Single((await service.GetAllAsync(Query(vehicleId: vehicle.Id), default)).Items);
        Assert.Single((await service.GetAllAsync(Query(driverId: driver.Id), default)).Items);
    }

    [Fact]
    public async Task Date_limits_are_inclusive_and_normalized()
    {
        await using var db = CreateDatabase(); db.Shipments.AddRange(Entity("A", Utc(8)), Entity("B", Utc(10)), Entity("C", Utc(12))); await db.SaveChangesAsync();
        var boundary = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 10, 0, 0), DateTimeKind.Unspecified);
        var result = await new ShipmentService(db).GetAllAsync(Query(from: boundary, to: boundary), default);
        Assert.Single(result.Items); Assert.Equal("B", result.Items[0].Reference);
    }

    [Fact]
    public async Task Pagination_has_stable_order_totals_and_empty_out_of_range_page()
    {
        await using var db = CreateDatabase(); db.Shipments.AddRange(Entity("B", Utc(10)), Entity("A", Utc(10)), Entity("C", Utc(11))); await db.SaveChangesAsync(); var service = new ShipmentService(db);
        var first = await service.GetAllAsync(Query(page: 1, pageSize: 2), default); var second = await service.GetAllAsync(Query(page: 2, pageSize: 2), default);
        Assert.Equal(["A", "B"], first.Items.Select(item => item.Reference)); Assert.Equal("C", Assert.Single(second.Items).Reference); Assert.Equal(3, first.TotalCount); Assert.Equal(2, first.TotalPages);
        Assert.Empty((await service.GetAllAsync(Query(page: 4, pageSize: 2), default)).Items);
        Assert.Empty((await service.GetAllAsync(Query(page: int.MaxValue, pageSize: 100), default)).Items);
    }

    private static UpsertShipmentRequest Request(string reference, DateTime? pickup = null, DateTime? delivery = null) =>
        new(reference, " Madrid ", " Barcelona ", pickup ?? Utc(8), delivery, null, 100, " note ");
    private static Shipment Entity(string reference, DateTime pickup) => new() { Reference = reference, Origin = "A", Destination = "B", PlannedPickupAt = pickup };
    private static DateTime Utc(int hour) => new(2026, 8, 1, hour, 0, 0, DateTimeKind.Utc);
    private static ListShipmentsQuery Query(string? status = null, DateTime? from = null, DateTime? to = null, Guid? customerId = null, Guid? vehicleId = null, Guid? driverId = null, int? page = null, int? pageSize = null) => new(status, from, to, customerId, vehicleId, driverId, page, pageSize);
    private static TransitOpsDbContext CreateDatabase() => new(new DbContextOptionsBuilder<TransitOpsDbContext>().UseInMemoryDatabase($"shipment-tests-{Guid.NewGuid():N}").Options);
}

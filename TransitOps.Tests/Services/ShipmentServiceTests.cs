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

    [Fact]
    public async Task Assign_persists_both_resources_and_returns_their_labels()
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); var vehicle = new Vehicle { LicensePlate = "1234 ABC", LoadCapacity = 2000 }; var driver = new Driver { Name = "Ana", LicenseNumber = "L-1" };
        db.AddRange(shipment, vehicle, driver); await db.SaveChangesAsync();
        var result = await new ShipmentService(db).AssignAsync(shipment.Id, new(vehicle.Id, driver.Id), default);
        Assert.Equal(vehicle.Id, result.VehicleId); Assert.Equal(driver.Id, result.DriverId); Assert.Equal("1234 ABC", result.VehiclePlate); Assert.Equal("Ana", result.DriverName); Assert.Null(result.CapacityWarning);
        var saved = await db.Shipments.SingleAsync(); Assert.Equal(vehicle.Id, saved.VehicleId); Assert.Equal(driver.Id, saved.DriverId);
    }

    [Theory]
    [InlineData(ShipmentStatus.InProgress, false)]
    [InlineData(ShipmentStatus.Delivered, false)]
    [InlineData(ShipmentStatus.Cancelled, false)]
    [InlineData(ShipmentStatus.InProgress, true)]
    [InlineData(ShipmentStatus.Delivered, true)]
    [InlineData(ShipmentStatus.Cancelled, true)]
    public async Task Assignment_changes_are_rejected_after_planned(ShipmentStatus status, bool unassign)
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); shipment.Status = status; var vehicle = new Vehicle { LicensePlate = "A" }; var driver = new Driver { Name = "Ana", LicenseNumber = "L" }; db.AddRange(shipment, vehicle, driver); await db.SaveChangesAsync(); var service = new ShipmentService(db);
        var error = await Assert.ThrowsAsync<ApiException>(() => unassign
            ? service.UnassignAsync(shipment.Id, default)
            : service.AssignAsync(shipment.Id, new(vehicle.Id, driver.Id), default));
        Assert.Equal(409, error.StatusCode); Assert.Equal("shipment_not_assignable", error.Code);
    }

    [Fact]
    public async Task Assignment_requires_complete_active_resources()
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); var activeVehicle = new Vehicle { LicensePlate = "A" }; var inactiveVehicle = new Vehicle { LicensePlate = "B", IsActive = false }; var activeDriver = new Driver { Name = "Ana", LicenseNumber = "L1" }; var inactiveDriver = new Driver { Name = "Berta", LicenseNumber = "L2", IsActive = false }; db.AddRange(shipment, activeVehicle, inactiveVehicle, activeDriver, inactiveDriver); await db.SaveChangesAsync(); var service = new ShipmentService(db);
        foreach (var request in new[] { new AssignShipmentRequest(activeVehicle.Id, null), new(null, activeDriver.Id), new AssignShipmentRequest(null, null) })
        { var error = await Assert.ThrowsAsync<ApiException>(() => service.AssignAsync(shipment.Id, request, default)); Assert.Equal(400, error.StatusCode); Assert.Equal("shipment_assignment_incomplete", error.Code); }
        foreach (var vehicleId in new[] { inactiveVehicle.Id, Guid.NewGuid() })
        { var error = await Assert.ThrowsAsync<ApiException>(() => service.AssignAsync(shipment.Id, new(vehicleId, activeDriver.Id), default)); Assert.Equal("shipment_vehicle_not_found", error.Code); }
        foreach (var driverId in new[] { inactiveDriver.Id, Guid.NewGuid() })
        { var error = await Assert.ThrowsAsync<ApiException>(() => service.AssignAsync(shipment.Id, new(activeVehicle.Id, driverId), default)); Assert.Equal("shipment_driver_not_found", error.Code); }
    }

    [Fact]
    public async Task Open_resources_are_busy_and_terminal_resources_are_released()
    {
        await using var db = CreateDatabase();
        var busyVehicle = new Vehicle { LicensePlate = "BUSY" }; var busyDriver = new Driver { Name = "Busy", LicenseNumber = "B" }; var releasedVehicle = new Vehicle { LicensePlate = "FREE" }; var releasedDriver = new Driver { Name = "Free", LicenseNumber = "F" };
        var vehicleOwner = Entity("VEHICLE-OWNER", Utc(8)); vehicleOwner.VehicleId = busyVehicle.Id; vehicleOwner.Status = ShipmentStatus.Planned;
        var driverOwner = Entity("DRIVER-OWNER", Utc(9)); driverOwner.DriverId = busyDriver.Id; driverOwner.Status = ShipmentStatus.InProgress;
        var completed = Entity("COMPLETED", Utc(10)); completed.VehicleId = releasedVehicle.Id; completed.DriverId = releasedDriver.Id; completed.Status = ShipmentStatus.Delivered;
        var target = Entity("TARGET", Utc(11)); db.AddRange(busyVehicle, busyDriver, releasedVehicle, releasedDriver, vehicleOwner, driverOwner, completed, target); await db.SaveChangesAsync(); var service = new ShipmentService(db);
        var vehicleError = await Assert.ThrowsAsync<ApiException>(() => service.AssignAsync(target.Id, new(busyVehicle.Id, releasedDriver.Id), default)); Assert.Equal("shipment_vehicle_busy", vehicleError.Code); Assert.Contains("VEHICLE-OWNER", vehicleError.Message);
        var driverError = await Assert.ThrowsAsync<ApiException>(() => service.AssignAsync(target.Id, new(releasedVehicle.Id, busyDriver.Id), default)); Assert.Equal("shipment_driver_busy", driverError.Code); Assert.Contains("DRIVER-OWNER", driverError.Message);
        var result = await service.AssignAsync(target.Id, new(releasedVehicle.Id, releasedDriver.Id), default); Assert.Equal(releasedVehicle.Id, result.VehicleId); Assert.Equal(releasedDriver.Id, result.DriverId);
    }

    [Fact]
    public async Task Reassigning_the_same_vehicle_excludes_the_current_shipment()
    {
        await using var db = CreateDatabase(); var vehicle = new Vehicle { LicensePlate = "A" }; var originalDriver = new Driver { Name = "Ana", LicenseNumber = "L1" }; var replacement = new Driver { Name = "Berta", LicenseNumber = "L2" }; var shipment = Entity("A", Utc(8)); shipment.VehicleId = vehicle.Id; shipment.DriverId = originalDriver.Id; db.AddRange(vehicle, originalDriver, replacement, shipment); await db.SaveChangesAsync();
        var result = await new ShipmentService(db).AssignAsync(shipment.Id, new(vehicle.Id, replacement.Id), default);
        Assert.Equal(vehicle.Id, result.VehicleId); Assert.Equal(replacement.Id, result.DriverId);
    }

    [Fact]
    public async Task Capacity_warning_never_blocks_and_requires_both_known_values()
    {
        await using var db = CreateDatabase(); var service = new ShipmentService(db);
        async Task<ShipmentResponse> Assign(string reference, decimal? capacity, decimal? load)
        {
            var vehicle = new Vehicle { LicensePlate = reference, LoadCapacity = capacity }; var driver = new Driver { Name = reference, LicenseNumber = reference }; var shipment = Entity(reference, Utc(8)); shipment.EstimatedLoad = load; db.AddRange(vehicle, driver, shipment); await db.SaveChangesAsync(); return await service.AssignAsync(shipment.Id, new(vehicle.Id, driver.Id), default);
        }
        var warning = await Assign("LOW", 3000, 4500); Assert.Contains("3000 kg", warning.CapacityWarning); Assert.Contains("4500 kg", warning.CapacityWarning); Assert.NotNull((await db.Shipments.SingleAsync(item => item.Id == warning.Id)).VehicleId);
        Assert.Null((await Assign("ENOUGH", 4500, 3000)).CapacityWarning); Assert.Null((await Assign("UNKNOWN-CAPACITY", null, 3000)).CapacityWarning); Assert.Null((await Assign("UNKNOWN-LOAD", 3000, null)).CapacityWarning);
    }

    [Theory]
    [InlineData(ShipmentStatus.Planned, "in_progress")]
    [InlineData(ShipmentStatus.Planned, "cancelled")]
    [InlineData(ShipmentStatus.InProgress, "delivered")]
    [InlineData(ShipmentStatus.InProgress, "cancelled")]
    public async Task Valid_status_transitions_follow_the_state_machine(ShipmentStatus current, string target)
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); shipment.Status = current; shipment.VehicleId = Guid.NewGuid(); shipment.DriverId = Guid.NewGuid(); if (current == ShipmentStatus.InProgress) shipment.ActualPickupAt = Utc(9); db.Shipments.Add(shipment); await db.SaveChangesAsync();
        var result = await new ShipmentService(db).ChangeStatusAsync(shipment.Id, new(target), default); Assert.Equal(target, result.Status);
    }

    [Theory]
    [InlineData(ShipmentStatus.Delivered, "cancelled")]
    [InlineData(ShipmentStatus.Cancelled, "delivered")]
    public async Task Terminal_statuses_cannot_change(ShipmentStatus current, string target)
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); shipment.Status = current; db.Shipments.Add(shipment); await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<ApiException>(() => new ShipmentService(db).ChangeStatusAsync(shipment.Id, new(target), default)); Assert.Equal(409, error.StatusCode); Assert.Equal("shipment_status_terminal", error.Code);
    }

    [Theory]
    [InlineData(ShipmentStatus.Planned, "planned")]
    [InlineData(ShipmentStatus.Planned, "delivered")]
    [InlineData(ShipmentStatus.InProgress, "in_progress")]
    public async Task Invalid_or_noop_status_transitions_are_rejected(ShipmentStatus current, string target)
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); shipment.Status = current; shipment.VehicleId = Guid.NewGuid(); shipment.DriverId = Guid.NewGuid(); db.Shipments.Add(shipment); await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<ApiException>(() => new ShipmentService(db).ChangeStatusAsync(shipment.Id, new(target), default)); Assert.Equal("shipment_status_transition_invalid", error.Code);
    }

    [Fact]
    public async Task Starting_requires_assignment_and_real_dates_are_sealed_in_utc()
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); db.Shipments.Add(shipment); await db.SaveChangesAsync(); var service = new ShipmentService(db);
        var missing = await Assert.ThrowsAsync<ApiException>(() => service.ChangeStatusAsync(shipment.Id, new("in_progress"), default)); Assert.Equal("shipment_assignment_required", missing.Code);
        shipment.VehicleId = Guid.NewGuid(); shipment.DriverId = Guid.NewGuid(); await db.SaveChangesAsync();
        var started = await service.ChangeStatusAsync(shipment.Id, new("in_progress"), default); Assert.NotNull(started.ActualPickupAt); Assert.Equal(DateTimeKind.Utc, started.ActualPickupAt!.Value.Kind); Assert.Null(started.ActualDeliveryAt); var pickup = started.ActualPickupAt;
        var delivered = await service.ChangeStatusAsync(shipment.Id, new("delivered"), default); Assert.Equal(pickup, delivered.ActualPickupAt); Assert.NotNull(delivered.ActualDeliveryAt); Assert.Equal(DateTimeKind.Utc, delivered.ActualDeliveryAt!.Value.Kind); Assert.True(delivered.ActualDeliveryAt >= delivered.ActualPickupAt);
    }

    [Fact]
    public async Task Cancelling_after_start_keeps_pickup_without_sealing_delivery()
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); shipment.Status = ShipmentStatus.InProgress; shipment.ActualPickupAt = Utc(9); db.Shipments.Add(shipment); await db.SaveChangesAsync();
        var result = await new ShipmentService(db).ChangeStatusAsync(shipment.Id, new("cancelled"), default); Assert.Equal(Utc(9), result.ActualPickupAt); Assert.Null(result.ActualDeliveryAt);
    }

    [Fact]
    public async Task Unassign_is_idempotent_and_operation_methods_return_404_for_missing_shipments()
    {
        await using var db = CreateDatabase(); var shipment = Entity("A", Utc(8)); db.Shipments.Add(shipment); await db.SaveChangesAsync(); var service = new ShipmentService(db); var updatedAt = shipment.UpdatedAt;
        var result = await service.UnassignAsync(shipment.Id, default); Assert.Null(result.VehicleId); Assert.Null(result.DriverId); Assert.Equal(updatedAt, result.UpdatedAt);
        foreach (var operation in new Func<Task>[] { () => service.AssignAsync(Guid.NewGuid(), new(Guid.NewGuid(), Guid.NewGuid()), default), () => service.UnassignAsync(Guid.NewGuid(), default), () => service.ChangeStatusAsync(Guid.NewGuid(), new("cancelled"), default) })
        { var error = await Assert.ThrowsAsync<ApiException>(operation); Assert.Equal(404, error.StatusCode); Assert.Equal("shipment_not_found", error.Code); }
    }

    private static UpsertShipmentRequest Request(string reference, DateTime? pickup = null, DateTime? delivery = null) =>
        new(reference, " Madrid ", " Barcelona ", pickup ?? Utc(8), delivery, null, 100, " note ");
    private static Shipment Entity(string reference, DateTime pickup) => new() { Reference = reference, Origin = "A", Destination = "B", PlannedPickupAt = pickup };
    private static DateTime Utc(int hour) => new(2026, 8, 1, hour, 0, 0, DateTimeKind.Utc);
    private static ListShipmentsQuery Query(string? status = null, DateTime? from = null, DateTime? to = null, Guid? customerId = null, Guid? vehicleId = null, Guid? driverId = null, int? page = null, int? pageSize = null) => new(status, from, to, customerId, vehicleId, driverId, page, pageSize);
    private static TransitOpsDbContext CreateDatabase() => new(new DbContextOptionsBuilder<TransitOpsDbContext>().UseInMemoryDatabase($"shipment-tests-{Guid.NewGuid():N}").Options);
}

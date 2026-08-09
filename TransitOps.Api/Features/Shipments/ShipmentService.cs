using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;
using TransitOps.Api.Security;

namespace TransitOps.Api.Features.Shipments;

public sealed class ShipmentService(
    TransitOpsDbContext dbContext,
    ICurrentUser currentUser) : IShipmentService
{
    public async Task<ShipmentPageResponse> GetAllAsync(ListShipmentsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 20;
        var pickupFrom = ShipmentTime.Utc(query.PickupFrom);
        var pickupTo = ShipmentTime.Utc(query.PickupTo);
        var items = dbContext.Shipments.AsNoTracking().AsQueryable();

        if (query.Status is not null)
        {
            var status = ShipmentStatuses.Parse(query.Status);
            items = items.Where(item => item.Status == status);
        }
        if (pickupFrom.HasValue) items = items.Where(item => item.PlannedPickupAt >= pickupFrom.Value);
        if (pickupTo.HasValue) items = items.Where(item => item.PlannedPickupAt <= pickupTo.Value);
        if (query.CustomerId.HasValue) items = items.Where(item => item.CustomerId == query.CustomerId);
        if (query.VehicleId.HasValue) items = items.Where(item => item.VehicleId == query.VehicleId);
        if (query.DriverId.HasValue) items = items.Where(item => item.DriverId == query.DriverId);

        var totalCount = await items.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page > totalPages) return new([], page, pageSize, totalCount, totalPages);

        var results = await items.OrderBy(item => item.PlannedPickupAt).ThenBy(item => item.Reference)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new
            {
                Item = item,
                CustomerName = dbContext.Customers.Where(value => value.Id == item.CustomerId)
                    .Select(value => value.Name).FirstOrDefault(),
                VehiclePlate = dbContext.Vehicles.Where(value => value.Id == item.VehicleId)
                    .Select(value => value.LicensePlate).FirstOrDefault(),
                DriverName = dbContext.Drivers.Where(value => value.Id == item.DriverId)
                    .Select(value => value.Name).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        return new(results.Select(value => Map(value.Item, value.CustomerName, value.VehiclePlate, value.DriverName)).ToList(),
            page, pageSize, totalCount, totalPages);
    }

    public async Task<ShipmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Detail(id, cancellationToken);
    }

    public async Task<ShipmentResponse> CreateAsync(UpsertShipmentRequest request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        await EnsureUnique(normalized.Reference, null, cancellationToken);
        await EnsureCustomer(normalized.CustomerId, cancellationToken);
        var item = new Shipment
        {
            Reference = normalized.Reference,
            Origin = normalized.Origin,
            Destination = normalized.Destination,
            PlannedPickupAt = normalized.PlannedPickupAt!.Value,
            PlannedDeliveryAt = normalized.PlannedDeliveryAt,
            CustomerId = normalized.CustomerId,
            EstimatedLoad = normalized.EstimatedLoad,
            Notes = normalized.Notes,
            Status = ShipmentStatus.Planned
        };
        dbContext.Shipments.Add(item);
        RecordAutomatic(item.Id, ShipmentEventType.Created);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (item.CustomerId.HasValue) await dbContext.Entry(item).Reference(value => value.Customer).LoadAsync(cancellationToken);
        return Map(item);
    }

    public async Task<ShipmentResponse> UpdateAsync(Guid id, UpsertShipmentRequest request, CancellationToken cancellationToken)
    {
        var item = await Existing(id, cancellationToken);
        var normalized = Normalize(request);
        await EnsureUnique(normalized.Reference, id, cancellationToken);
        if (normalized.CustomerId != item.CustomerId) await EnsureCustomer(normalized.CustomerId, cancellationToken);
        item.Reference = normalized.Reference;
        item.Origin = normalized.Origin;
        item.Destination = normalized.Destination;
        item.PlannedPickupAt = normalized.PlannedPickupAt!.Value;
        item.PlannedDeliveryAt = normalized.PlannedDeliveryAt;
        item.CustomerId = normalized.CustomerId;
        item.EstimatedLoad = normalized.EstimatedLoad;
        item.Notes = normalized.Notes;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await Detail(id, cancellationToken))!;
    }

    public async Task<ShipmentResponse> AssignAsync(Guid id, AssignShipmentRequest request, CancellationToken cancellationToken)
    {
        var item = await Existing(id, cancellationToken);
        EnsureAssignable(item);
        if (!request.VehicleId.HasValue || !request.DriverId.HasValue)
            throw new ApiException(400, "shipment_assignment_incomplete", "Hay que indicar vehículo y conductor.");

        var vehicle = await EnsureVehicle(request.VehicleId.Value, cancellationToken);
        var driver = await EnsureDriver(request.DriverId.Value, cancellationToken);
        await EnsureVehicleNotBusy(id, request.VehicleId.Value, cancellationToken);
        await EnsureDriverNotBusy(id, request.DriverId.Value, cancellationToken);

        item.VehicleId = request.VehicleId.Value;
        item.DriverId = request.DriverId.Value;
        item.UpdatedAt = DateTime.UtcNow;
        RecordAutomatic(
            item.Id,
            ShipmentEventType.Assigned,
            $"Vehículo {vehicle.LicensePlate} · Conductor {driver.Name}");
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            var conflict = MapAssignmentConflict(exception);
            if (conflict is null)
                throw;
            throw conflict;
        }

        var warning = vehicle.LoadCapacity.HasValue && item.EstimatedLoad.HasValue &&
                      vehicle.LoadCapacity.Value < item.EstimatedLoad.Value
            ? $"La capacidad del vehículo ({FormatLoad(vehicle.LoadCapacity.Value)} kg) es inferior a la carga estimada ({FormatLoad(item.EstimatedLoad.Value)} kg)."
            : null;
        return Map(item, vehiclePlate: vehicle.LicensePlate, driverName: driver.Name, capacityWarning: warning);
    }

    public async Task<ShipmentResponse> UnassignAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await Existing(id, cancellationToken);
        EnsureAssignable(item);
        if (!item.VehicleId.HasValue && !item.DriverId.HasValue) return Map(item);
        item.VehicleId = null;
        item.DriverId = null;
        item.UpdatedAt = DateTime.UtcNow;
        RecordAutomatic(item.Id, ShipmentEventType.Unassigned);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<ShipmentResponse> ChangeStatusAsync(Guid id, ChangeShipmentStatusRequest request, CancellationToken cancellationToken)
    {
        var item = await Existing(id, cancellationToken);
        var target = ShipmentStatuses.Parse(request.Status);
        var eventType = EnsureTransition(item, target);

        var now = DateTime.UtcNow;
        if (target == ShipmentStatus.InProgress) item.ActualPickupAt = now;
        if (target == ShipmentStatus.Delivered) item.ActualDeliveryAt = now;
        item.Status = target;
        item.UpdatedAt = now;
        RecordAutomatic(item.Id, eventType, occurredAt: now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await Detail(id, cancellationToken))!;
    }

    private static UpsertShipmentRequest Normalize(UpsertShipmentRequest request)
    {
        if (!request.PlannedPickupAt.HasValue)
            throw new ApiException(400, "shipment_pickup_required", "La fecha de recogida prevista es obligatoria.");
        var normalized = request with
        {
            Reference = request.Reference.Trim().ToUpperInvariant(),
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            PlannedPickupAt = ShipmentTime.Utc(request.PlannedPickupAt),
            PlannedDeliveryAt = ShipmentTime.Utc(request.PlannedDeliveryAt),
            Notes = Optional(request.Notes)
        };
        if (normalized.PlannedDeliveryAt.HasValue && normalized.PlannedDeliveryAt.Value < normalized.PlannedPickupAt!.Value)
            throw new ApiException(400, "shipment_dates_invalid", "La entrega prevista no puede ser anterior a la recogida.");
        return normalized;
    }

    private async Task EnsureUnique(string reference, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await dbContext.Shipments.AnyAsync(item => (!excludedId.HasValue || item.Id != excludedId.Value) && item.Reference == reference, cancellationToken))
            throw new ApiException(409, "shipment_reference_conflict", "Ya existe un envío con esa referencia.");
    }

    private async Task EnsureCustomer(Guid? customerId, CancellationToken cancellationToken)
    {
        if (customerId.HasValue && !await dbContext.Customers.AnyAsync(item => item.Id == customerId.Value && item.IsActive, cancellationToken))
            throw new ApiException(409, "shipment_customer_not_found", "El cliente indicado no existe o está dado de baja.");
    }

    private async Task<VehicleAssignment> EnsureVehicle(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Vehicles.AsNoTracking().Where(item => item.Id == id && item.IsActive)
            .Select(item => new VehicleAssignment { LicensePlate = item.LicensePlate, LoadCapacity = item.LoadCapacity })
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new ApiException(409, "shipment_vehicle_not_found", "El vehículo indicado no existe o está dado de baja.");

    private async Task<DriverAssignment> EnsureDriver(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Drivers.AsNoTracking().Where(item => item.Id == id && item.IsActive)
            .Select(item => new DriverAssignment { Name = item.Name })
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new ApiException(409, "shipment_driver_not_found", "El conductor indicado no existe o está dado de baja.");

    private async Task EnsureVehicleNotBusy(Guid shipmentId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var conflict = await dbContext.Shipments.AsNoTracking()
            .Where(item => item.Id != shipmentId && item.VehicleId == vehicleId &&
                (item.Status == ShipmentStatus.Planned || item.Status == ShipmentStatus.InProgress))
            .Select(item => item.Reference).FirstOrDefaultAsync(cancellationToken);
        if (conflict is not null)
            throw new ApiException(409, "shipment_vehicle_busy", $"El vehículo ya está asignado al envío {conflict}.");
    }

    private async Task EnsureDriverNotBusy(Guid shipmentId, Guid driverId, CancellationToken cancellationToken)
    {
        var conflict = await dbContext.Shipments.AsNoTracking()
            .Where(item => item.Id != shipmentId && item.DriverId == driverId &&
                (item.Status == ShipmentStatus.Planned || item.Status == ShipmentStatus.InProgress))
            .Select(item => item.Reference).FirstOrDefaultAsync(cancellationToken);
        if (conflict is not null)
            throw new ApiException(409, "shipment_driver_busy", $"El conductor ya está asignado al envío {conflict}.");
    }

    private static ApiException? MapAssignmentConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
            return null;

        return postgres.ConstraintName switch
        {
            TransitOpsDbContext.OpenShipmentVehicleIndex => new ApiException(
                409,
                "shipment_vehicle_busy",
                "El vehículo ya está asignado a otro envío."),
            TransitOpsDbContext.OpenShipmentDriverIndex => new ApiException(
                409,
                "shipment_driver_busy",
                "El conductor ya está asignado a otro envío."),
            _ => null
        };
    }

    private static void EnsureAssignable(Shipment item)
    {
        if (item.Status != ShipmentStatus.Planned)
            throw new ApiException(409, "shipment_not_assignable", "Solo se puede asignar mientras el envío está planificado.");
    }

    private static ShipmentEventType EnsureTransition(Shipment item, ShipmentStatus target)
    {
        if (item.Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled)
            throw new ApiException(409, "shipment_status_terminal", "Un envío entregado o cancelado no puede cambiar de estado.");
        if (item.Status == ShipmentStatus.Planned && target == ShipmentStatus.InProgress &&
            (!item.VehicleId.HasValue || !item.DriverId.HasValue))
            throw new ApiException(409, "shipment_assignment_required", "Para poner el envío en curso hay que asignar vehículo y conductor.");

        return (item.Status, target) switch
        {
            (ShipmentStatus.Planned, ShipmentStatus.InProgress) => ShipmentEventType.Departed,
            (ShipmentStatus.Planned, ShipmentStatus.Cancelled) => ShipmentEventType.Cancelled,
            (ShipmentStatus.InProgress, ShipmentStatus.Delivered) => ShipmentEventType.Delivered,
            (ShipmentStatus.InProgress, ShipmentStatus.Cancelled) => ShipmentEventType.Cancelled,
            _ => throw new ApiException(409, "shipment_status_transition_invalid", "La transición de estado solicitada no es válida.")
        };
    }

    private async Task<ShipmentResponse?> Detail(Guid id, CancellationToken cancellationToken)
    {
        var result = await dbContext.Shipments.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new
            {
                Item = item,
                CustomerName = dbContext.Customers.Where(value => value.Id == item.CustomerId)
                    .Select(value => value.Name).FirstOrDefault(),
                VehiclePlate = dbContext.Vehicles.Where(value => value.Id == item.VehicleId)
                    .Select(value => value.LicensePlate).FirstOrDefault(),
                DriverName = dbContext.Drivers.Where(value => value.Id == item.DriverId)
                    .Select(value => value.Name).FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);
        return result is null ? null : Map(result.Item, result.CustomerName, result.VehiclePlate, result.DriverName);
    }

    private async Task<Shipment> Existing(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Shipments.Include(item => item.Customer).SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
        ?? throw new ApiException(404, "shipment_not_found", "El envío no existe.");

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RecordAutomatic(
        Guid shipmentId,
        ShipmentEventType eventType,
        string? notes = null,
        DateTime? occurredAt = null) =>
        dbContext.ShipmentEvents.Add(new ShipmentEvent
        {
            ShipmentId = shipmentId,
            EventType = eventType,
            OccurredAt = occurredAt ?? DateTime.UtcNow,
            Notes = notes,
            RecordedByUserId = currentUser.Id
        });

    private static string FormatLoad(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static ShipmentResponse Map(Shipment item, string? customerName = null, string? vehiclePlate = null,
        string? driverName = null, string? capacityWarning = null) =>
        new(item.Id, item.Reference, item.Origin, item.Destination, item.PlannedPickupAt, item.PlannedDeliveryAt,
            item.CustomerId, customerName ?? item.Customer?.Name, item.EstimatedLoad, item.Notes,
            ShipmentStatuses.Token(item.Status), item.VehicleId, item.DriverId, vehiclePlate, driverName,
            item.ActualPickupAt, item.ActualDeliveryAt, capacityWarning, item.CreatedAt, item.UpdatedAt);

    private sealed class VehicleAssignment
    {
        public required string LicensePlate { get; init; }
        public decimal? LoadCapacity { get; init; }
    }

    private sealed class DriverAssignment
    {
        public required string Name { get; init; }
    }
}

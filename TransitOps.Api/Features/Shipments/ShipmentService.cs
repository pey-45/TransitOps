using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Features.Shipments;

public sealed class ShipmentService(TransitOpsDbContext dbContext) : IShipmentService
{
    public async Task<ShipmentPageResponse> GetAllAsync(ListShipmentsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 20;
        var pickupFrom = ShipmentTime.Utc(query.PickupFrom);
        var pickupTo = ShipmentTime.Utc(query.PickupTo);
        var items = dbContext.Shipments.AsNoTracking().Include(item => item.Customer).AsQueryable();

        if (query.Status is not null)
        {
            var status = query.Status switch
            {
                "planned" => ShipmentStatus.Planned,
                "in_progress" => ShipmentStatus.InProgress,
                "delivered" => ShipmentStatus.Delivered,
                "cancelled" => ShipmentStatus.Cancelled,
                _ => throw new ApiException(400, "shipment_status_invalid", "El estado indicado no es válido.")
            };
            items = items.Where(item => item.Status == status);
        }
        if (pickupFrom.HasValue) items = items.Where(item => item.PlannedPickupAt >= pickupFrom.Value);
        if (pickupTo.HasValue) items = items.Where(item => item.PlannedPickupAt <= pickupTo.Value);
        if (query.CustomerId.HasValue) items = items.Where(item => item.CustomerId == query.CustomerId);
        if (query.VehicleId.HasValue) items = items.Where(item => item.VehicleId == query.VehicleId);
        if (query.DriverId.HasValue) items = items.Where(item => item.DriverId == query.DriverId);

        var totalCount = await items.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var results = page > totalPages
            ? []
            : await items.OrderBy(item => item.PlannedPickupAt).ThenBy(item => item.Reference)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new(results.Select(Map).ToList(), page, pageSize, totalCount, totalPages);
    }

    public async Task<ShipmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Shipments.AsNoTracking().Include(item => item.Customer)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return item is null ? null : Map(item);
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
        var customerName = item.CustomerId.HasValue
            ? await dbContext.Customers.AsNoTracking().Where(value => value.Id == item.CustomerId.Value)
                .Select(value => value.Name).SingleAsync(cancellationToken)
            : null;
        return Map(item) with { CustomerName = customerName };
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

    private async Task<Shipment> Existing(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Shipments.Include(item => item.Customer).SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
        ?? throw new ApiException(404, "shipment_not_found", "El envío no existe.");

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ShipmentResponse Map(Shipment item) => new(item.Id, item.Reference, item.Origin, item.Destination,
        item.PlannedPickupAt, item.PlannedDeliveryAt, item.CustomerId, item.Customer?.Name, item.EstimatedLoad,
        item.Notes, item.Status switch
        {
            ShipmentStatus.Planned => "planned",
            ShipmentStatus.InProgress => "in_progress",
            ShipmentStatus.Delivered => "delivered",
            ShipmentStatus.Cancelled => "cancelled",
            _ => throw new InvalidOperationException("Estado de envío desconocido.")
        }, item.VehicleId, item.DriverId, item.CreatedAt, item.UpdatedAt);
}

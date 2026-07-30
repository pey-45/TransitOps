using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;
using TransitOps.Api.Security;

namespace TransitOps.Api.Features.Shipments;

public sealed class ShipmentEventService(
    TransitOpsDbContext dbContext,
    ICurrentUser currentUser) : IShipmentEventService
{
    public async Task<IReadOnlyList<ShipmentEventResponse>> GetByShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken)
    {
        await EnsureShipmentExists(shipmentId, cancellationToken);

        var results = await dbContext.ShipmentEvents.AsNoTracking()
            .Where(item => item.ShipmentId == shipmentId)
            .OrderBy(item => item.OccurredAt)
            .ThenBy(item => item.CreatedAt)
            .Select(item => new
            {
                Item = item,
                RecordedByUsername = dbContext.AppUsers
                    .Where(user => user.Id == item.RecordedByUserId)
                    .Select(user => user.Username)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return results.Select(result => Map(result.Item, result.RecordedByUsername)).ToList();
    }

    public async Task<ShipmentEventResponse> CreateAsync(
        Guid shipmentId,
        CreateShipmentEventRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureShipmentExists(shipmentId, cancellationToken);

        var occurredAt = ShipmentTime.Utc(request.OccurredAt) ?? DateTime.UtcNow;
        if (occurredAt > DateTime.UtcNow.Add(CreateShipmentEventRequest.FutureTolerance))
        {
            throw new ApiException(
                400,
                "shipment_event_future",
                "La fecha del evento no puede estar en el futuro.");
        }

        var item = new ShipmentEvent
        {
            ShipmentId = shipmentId,
            EventType = ShipmentEventTypes.Parse(request.EventType),
            OccurredAt = occurredAt,
            Location = Optional(request.Location),
            Notes = Optional(request.Notes),
            RecordedByUserId = currentUser.Id
        };
        dbContext.ShipmentEvents.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        var username = item.RecordedByUserId.HasValue
            ? await dbContext.AppUsers.AsNoTracking()
                .Where(user => user.Id == item.RecordedByUserId.Value)
                .Select(user => user.Username)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        return Map(item, username);
    }

    private async Task EnsureShipmentExists(Guid shipmentId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Shipments.AsNoTracking()
            .AnyAsync(item => item.Id == shipmentId, cancellationToken))
        {
            throw new ApiException(404, "shipment_not_found", "El envío no existe.");
        }
    }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ShipmentEventResponse Map(ShipmentEvent item, string? recordedByUsername) =>
        new(
            item.Id,
            item.ShipmentId,
            ShipmentEventTypes.Token(item.EventType),
            item.OccurredAt,
            item.Location,
            item.Notes,
            item.RecordedByUserId,
            recordedByUsername,
            item.CreatedAt);
}

using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Application.ShipmentEvents;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.ShipmentEvents;
using TransitOps.Api.Contracts.Responses.ShipmentEvents;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Errors;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Api.Infrastructure.ShipmentEvents;

public sealed class ShipmentEventService : IShipmentEventService
{
    private readonly TransitOpsDbContext _dbContext;

    public ShipmentEventService(TransitOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ShipmentEventResponse>> GetByTransportIdAsync(
        Guid transportId,
        CancellationToken cancellationToken)
    {
        await EnsureActiveTransportExistsAsync(transportId, cancellationToken);

        return await _dbContext.ShipmentEvents
            .AsNoTracking()
            .Where(shipmentEvent => shipmentEvent.TransportId == transportId && shipmentEvent.DeletedAt == null)
            .OrderBy(shipmentEvent => shipmentEvent.EventDate)
            .ThenBy(shipmentEvent => shipmentEvent.CreatedAt)
            .ThenBy(shipmentEvent => shipmentEvent.Id)
            .Join(
                _dbContext.AppUsers.AsNoTracking(),
                shipmentEvent => shipmentEvent.CreatedByUserId,
                appUser => appUser.Id,
                (shipmentEvent, appUser) => new ShipmentEventResponse(
                    shipmentEvent.Id,
                    shipmentEvent.TransportId,
                    shipmentEvent.CreatedByUserId,
                    new ShipmentEventActorResponse(
                        appUser.Id,
                        appUser.Username,
                        appUser.Email,
                        appUser.UserRole),
                    shipmentEvent.EventType,
                    shipmentEvent.EventDate,
                    shipmentEvent.Location,
                    shipmentEvent.Notes,
                    shipmentEvent.CreatedAt,
                    shipmentEvent.DeletedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ShipmentEventResponse> CreateAsync(
        Guid transportId,
        Guid actorId,
        CreateShipmentEventRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureActiveTransportExistsAsync(transportId, cancellationToken);
        var actor = await GetActiveActorAsync(actorId, cancellationToken);

        var shipmentEvent = new ShipmentEvent
        {
            TransportId = transportId,
            CreatedByUserId = actor.Id,
            EventType = request.ParseEventType(),
            EventDate = DateTimePersistence.AsUnspecified(request.EventDate),
            Location = NormalizeOptionalText(request.Location),
            Notes = NormalizeOptionalText(request.Notes)
        };

        _dbContext.ShipmentEvents.Add(shipmentEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(shipmentEvent, actor);
    }

    private async Task EnsureActiveTransportExistsAsync(
        Guid transportId,
        CancellationToken cancellationToken)
    {
        var transportExists = await _dbContext.Transports
            .AnyAsync(
                transport => transport.Id == transportId && transport.DeletedAt == null,
                cancellationToken);

        if (!transportExists)
        {
            throw new ResourceNotFoundException(
                "transport_not_found",
                $"Transport '{transportId}' was not found.");
        }
    }

    private async Task<AppUser> GetActiveActorAsync(
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var actor = await _dbContext.AppUsers
            .SingleOrDefaultAsync(
                appUser => appUser.Id == actorId && appUser.DeletedAt == null,
                cancellationToken);

        if (actor is null)
        {
            throw new ResourceNotFoundException(
                "shipment_event_actor_not_found",
                $"User '{actorId}' was not found.");
        }

        if (!actor.IsActive)
        {
            throw new ConflictException(
                "shipment_event_actor_inactive",
                $"User '{actorId}' is inactive and cannot create shipment events.");
        }

        return actor;
    }

    private static ShipmentEventResponse MapToResponse(ShipmentEvent shipmentEvent, AppUser actor)
    {
        return new ShipmentEventResponse(
            shipmentEvent.Id,
            shipmentEvent.TransportId,
            shipmentEvent.CreatedByUserId,
            new ShipmentEventActorResponse(
                actor.Id,
                actor.Username,
                actor.Email,
                actor.UserRole),
            shipmentEvent.EventType,
            shipmentEvent.EventDate,
            shipmentEvent.Location,
            shipmentEvent.Notes,
            shipmentEvent.CreatedAt,
            shipmentEvent.DeletedAt);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

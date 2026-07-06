using TransitOps.Api.Contracts.Requests.ShipmentEvents;
using TransitOps.Api.Contracts.Responses.ShipmentEvents;

namespace TransitOps.Api.Application.ShipmentEvents;

public interface IShipmentEventService
{
    Task<IReadOnlyList<ShipmentEventResponse>> GetByTransportIdAsync(
        Guid transportId,
        CancellationToken cancellationToken);

    Task<ShipmentEventResponse> CreateAsync(
        Guid transportId,
        Guid actorId,
        CreateShipmentEventRequest request,
        CancellationToken cancellationToken);
}

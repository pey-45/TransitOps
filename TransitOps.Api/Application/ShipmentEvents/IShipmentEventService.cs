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
        CreateShipmentEventRequest request,
        CancellationToken cancellationToken);
}

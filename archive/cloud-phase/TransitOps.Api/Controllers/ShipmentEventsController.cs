using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.ShipmentEvents;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.ShipmentEvents;
using TransitOps.Api.Contracts.Responses.ShipmentEvents;

namespace TransitOps.Api.Controllers;

[Route("api/v1/transports/{transportId:guid}/shipment-events")]
public sealed class ShipmentEventsController : ApiControllerBase
{
    private readonly IShipmentEventService _shipmentEventService;

    public ShipmentEventsController(IShipmentEventService shipmentEventService)
    {
        _shipmentEventService = shipmentEventService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShipmentEventResponse>>>> GetByTransportId(
        Guid transportId,
        CancellationToken cancellationToken)
    {
        var shipmentEvents = await _shipmentEventService.GetByTransportIdAsync(transportId, cancellationToken);

        return OkResponse<IReadOnlyList<ShipmentEventResponse>>(shipmentEvents);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShipmentEventResponse>>> Create(
        Guid transportId,
        [FromBody] CreateShipmentEventRequest request,
        CancellationToken cancellationToken)
    {
        var shipmentEvent = await _shipmentEventService.CreateAsync(
            transportId,
            GetRequiredUserId(),
            request,
            cancellationToken);

        return Created(
            $"/api/v1/transports/{transportId}/shipment-events/{shipmentEvent.Id}",
            ApiResponse<ShipmentEventResponse>.Success(shipmentEvent, HttpContext.TraceIdentifier));
    }
}

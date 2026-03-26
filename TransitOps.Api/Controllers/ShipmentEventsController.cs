using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Responses.ShipmentEvents;

namespace TransitOps.Api.Controllers;

[Route("api/v1/transports/{transportId:guid}/shipment-events")]
public sealed class ShipmentEventsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<ShipmentEventResponse>>> GetByTransportId(Guid transportId)
    {
        return OkResponse<IReadOnlyList<ShipmentEventResponse>>(Array.Empty<ShipmentEventResponse>());
    }
}

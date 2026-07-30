using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Shipments;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/shipments/{shipmentId:guid}/events")]
[Authorize(Policy = Policies.Operational)]
public sealed class ShipmentEventsController(IShipmentEventService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShipmentEventResponse>>>> GetAll(
        Guid shipmentId,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<ShipmentEventResponse>>.Success(
            await service.GetByShipmentAsync(shipmentId, cancellationToken),
            HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ShipmentEventResponse>>> Create(
        Guid shipmentId,
        CreateShipmentEventRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<ShipmentEventResponse>.Success(
                await service.CreateAsync(shipmentId, request, cancellationToken),
                HttpContext.TraceIdentifier));
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Shipments;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/shipments")]
[Authorize(Policy = Policies.Operational)]
public sealed class ShipmentsController(IShipmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ShipmentPageResponse>>> GetAll([FromQuery] ListShipmentsQuery query, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ShipmentPageResponse>.Success(await service.GetAllAsync(query, cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShipmentResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(id, cancellationToken)
            ?? throw new ApiException(404, "shipment_not_found", "El envío no existe.");
        return Ok(ApiResponse<ShipmentResponse>.Success(item, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ShipmentResponse>>> Create(UpsertShipmentRequest request, CancellationToken cancellationToken) =>
        StatusCode(201, ApiResponse<ShipmentResponse>.Success(await service.CreateAsync(request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShipmentResponse>>> Update(Guid id, UpsertShipmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ShipmentResponse>.Success(await service.UpdateAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPut("{id:guid}/assignment")]
    public async Task<ActionResult<ApiResponse<ShipmentResponse>>> Assign(Guid id, AssignShipmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ShipmentResponse>.Success(await service.AssignAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("{id:guid}/assignment")]
    public async Task<ActionResult<ApiResponse<ShipmentResponse>>> Unassign(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ShipmentResponse>.Success(await service.UnassignAsync(id, cancellationToken), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<ShipmentResponse>>> ChangeStatus(Guid id, ChangeShipmentStatusRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ShipmentResponse>.Success(await service.ChangeStatusAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));
}

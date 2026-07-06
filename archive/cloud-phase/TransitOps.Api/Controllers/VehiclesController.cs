using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.Vehicles;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Vehicles;
using TransitOps.Api.Contracts.Responses.Vehicles;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class VehiclesController : ApiControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<VehicleResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleService.GetAllAsync(cancellationToken);

        return OkResponse<IReadOnlyList<VehicleResponse>>(vehicles);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id, cancellationToken);

        if (vehicle is null)
        {
            throw new ResourceNotFoundException("vehicle_not_found", $"Vehicle '{id}' was not found.");
        }

        return OkResponse(vehicle);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> Create(
        [FromBody] UpsertVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = vehicle.Id },
            ApiResponse<VehicleResponse>.Success(vehicle, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> Update(
        Guid id,
        [FromBody] UpsertVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.UpdateAsync(id, request, cancellationToken);

        return OkResponse(vehicle);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _vehicleService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.Drivers;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Drivers;
using TransitOps.Api.Contracts.Responses.Drivers;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class DriversController : ApiControllerBase
{
    private readonly IDriverService _driverService;

    public DriversController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DriverResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var drivers = await _driverService.GetAllAsync(cancellationToken);

        return OkResponse<IReadOnlyList<DriverResponse>>(drivers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var driver = await _driverService.GetByIdAsync(id, cancellationToken);

        if (driver is null)
        {
            throw new ResourceNotFoundException("driver_not_found", $"Driver '{id}' was not found.");
        }

        return OkResponse(driver);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> Create(
        [FromBody] UpsertDriverRequest request,
        CancellationToken cancellationToken)
    {
        var driver = await _driverService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = driver.Id },
            ApiResponse<DriverResponse>.Success(driver, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> Update(
        Guid id,
        [FromBody] UpsertDriverRequest request,
        CancellationToken cancellationToken)
    {
        var driver = await _driverService.UpdateAsync(id, request, cancellationToken);

        return OkResponse(driver);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _driverService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}

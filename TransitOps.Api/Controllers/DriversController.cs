using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Drivers;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/drivers")]
[Authorize(Policy = Policies.Operational)]
public sealed class DriversController(IDriverService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DriverResponse>>>> GetAll(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<DriverResponse>>.Success(await service.GetAllAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(id, cancellationToken)
            ?? throw new ApiException(404, "driver_not_found", "El conductor no existe o está dado de baja.");
        return Ok(ApiResponse<DriverResponse>.Success(item, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> Create(UpsertDriverRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<DriverResponse>.Success(item, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DriverResponse>>> Update(Guid id, UpsertDriverRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<DriverResponse>.Success(await service.UpdateAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { deactivated = true }, HttpContext.TraceIdentifier));
    }
}

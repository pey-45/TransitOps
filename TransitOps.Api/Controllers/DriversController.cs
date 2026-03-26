using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Responses.Drivers;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class DriversController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<DriverResponse>>> GetAll()
    {
        return OkResponse<IReadOnlyList<DriverResponse>>(Array.Empty<DriverResponse>());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<DriverResponse>> GetById(Guid id)
    {
        throw new ResourceNotFoundException("driver_not_found", $"Driver '{id}' was not found.");
    }
}

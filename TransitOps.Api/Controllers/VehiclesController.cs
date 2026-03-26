using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Responses.Vehicles;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class VehiclesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<VehicleResponse>>> GetAll()
    {
        return OkResponse<IReadOnlyList<VehicleResponse>>(Array.Empty<VehicleResponse>());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<VehicleResponse>> GetById(Guid id)
    {
        throw new ResourceNotFoundException("vehicle_not_found", $"Vehicle '{id}' was not found.");
    }
}

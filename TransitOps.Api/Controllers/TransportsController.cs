using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Responses.Transports;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class TransportsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<TransportSummaryResponse>>> GetAll()
    {
        return OkResponse<IReadOnlyList<TransportSummaryResponse>>(Array.Empty<TransportSummaryResponse>());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<TransportDetailResponse>> GetById(Guid id)
    {
        throw new ResourceNotFoundException("transport_not_found", $"Transport '{id}' was not found.");
    }
}

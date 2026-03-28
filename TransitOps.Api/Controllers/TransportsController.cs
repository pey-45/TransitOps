using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.Queries.Transports;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Responses.Transports;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class TransportsController : ApiControllerBase
{
    private readonly ITransportQueries _transportQueries;

    public TransportsController(ITransportQueries transportQueries)
    {
        _transportQueries = transportQueries;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TransportSummaryResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var transports = await _transportQueries.GetAllAsync(cancellationToken);

        return OkResponse<IReadOnlyList<TransportSummaryResponse>>(transports);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransportDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transport = await _transportQueries.GetByIdAsync(id, cancellationToken);

        if (transport is null)
        {
            throw new ResourceNotFoundException("transport_not_found", $"Transport '{id}' was not found.");
        }

        return OkResponse(transport);
    }
}

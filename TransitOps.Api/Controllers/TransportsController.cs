using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.Transports;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Transports;
using TransitOps.Api.Contracts.Responses.Transports;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class TransportsController : ApiControllerBase
{
    private readonly ITransportService _transportService;

    public TransportsController(ITransportService transportService)
    {
        _transportService = transportService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TransportSummaryResponse>>>> GetAll(
        [FromQuery] GetTransportsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transportService.GetAllAsync(request, cancellationToken);

        return OkResponse<IReadOnlyList<TransportSummaryResponse>>(
            result.Items,
            result.ToPaginationMetadata());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransportDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transport = await _transportService.GetByIdAsync(id, cancellationToken);

        if (transport is null)
        {
            throw new ResourceNotFoundException("transport_not_found", $"Transport '{id}' was not found.");
        }

        return OkResponse(transport);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TransportDetailResponse>>> Create(
        [FromBody] UpsertTransportRequest request,
        CancellationToken cancellationToken)
    {
        var transport = await _transportService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = transport.Id },
            ApiResponse<TransportDetailResponse>.Success(transport, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TransportDetailResponse>>> Update(
        Guid id,
        [FromBody] UpsertTransportRequest request,
        CancellationToken cancellationToken)
    {
        var transport = await _transportService.UpdateAsync(id, request, cancellationToken);

        return OkResponse(transport);
    }

    [HttpPut("{id:guid}/assignment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TransportDetailResponse>>> Assign(
        Guid id,
        [FromBody] AssignTransportRequest request,
        CancellationToken cancellationToken)
    {
        var transport = await _transportService.AssignAsync(id, request, cancellationToken);

        return OkResponse(transport);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _transportService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}

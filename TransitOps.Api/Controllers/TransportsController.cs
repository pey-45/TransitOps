using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.Commands.Transports;
using TransitOps.Api.Application.Queries.Transports;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Transports;
using TransitOps.Api.Contracts.Responses.Transports;
using TransitOps.Api.Errors;

namespace TransitOps.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class TransportsController : ApiControllerBase
{
    private readonly ITransportCommands _transportCommands;
    private readonly ITransportQueries _transportQueries;

    public TransportsController(
        ITransportCommands transportCommands,
        ITransportQueries transportQueries)
    {
        _transportCommands = transportCommands;
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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<TransportDetailResponse>>> Create(
        [FromBody] UpsertTransportRequest request,
        CancellationToken cancellationToken)
    {
        var transport = await _transportCommands.CreateAsync(request, cancellationToken);

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
        var transport = await _transportCommands.UpdateAsync(id, request, cancellationToken);

        return OkResponse(transport);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _transportCommands.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}

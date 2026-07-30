using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Reporting;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/summary")]
[Authorize(Policy = Policies.Operational)]
public sealed class SummaryController(ISummaryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<SummaryResponse>>> Get(
        [FromQuery] SummaryQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<SummaryResponse>.Success(
            await service.GetAsync(query, cancellationToken),
            HttpContext.TraceIdentifier));
}

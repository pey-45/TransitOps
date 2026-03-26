using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Controllers;

[Route("api/v1/health")]
public sealed class HealthController : ApiControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly TransitOpsDbContext _dbContext;

    public HealthController(IHostEnvironment environment, TransitOpsDbContext dbContext)
    {
        _environment = environment;
        _dbContext = dbContext;
    }

    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> Live()
    {
        return OkResponse<object>(new
        {
            status = "live",
            service = "TransitOps.Api",
            environment = _environment.EnvironmentName
        });
    }

    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<object>>> Ready(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiErrorResponse.Create(
                    code: "database_unavailable",
                    message: "The API cannot connect to PostgreSQL.",
                    requestId: HttpContext.TraceIdentifier));
        }

        return OkResponse<object>(new
        {
            status = "ready",
            service = "TransitOps.Api",
            environment = _environment.EnvironmentName,
            databaseConnectionAvailable = true
        });
    }
}

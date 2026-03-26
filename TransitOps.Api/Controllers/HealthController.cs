using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;

namespace TransitOps.Api.Controllers;

[Route("api/v1/health")]
public sealed class HealthController : ApiControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public HealthController(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
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
    public ActionResult<ApiResponse<object>> Ready()
    {
        var hasConnectionString = !string.IsNullOrWhiteSpace(
            _configuration.GetConnectionString("DefaultConnection"));

        return OkResponse<object>(new
        {
            status = "ready",
            service = "TransitOps.Api",
            environment = _environment.EnvironmentName,
            databaseConfigurationPresent = hasConnectionString
        });
    }
}

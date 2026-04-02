using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;

namespace TransitOps.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> OkResponse<T>(
        T data,
        ApiPaginationMetadata? pagination = null)
    {
        return Ok(ApiResponse<T>.Success(data, HttpContext.TraceIdentifier, pagination));
    }
}

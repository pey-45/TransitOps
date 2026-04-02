using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Errors;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.OperationalAccess)]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> OkResponse<T>(
        T data,
        ApiPaginationMetadata? pagination = null)
    {
        return Ok(ApiResponse<T>.Success(data, HttpContext.TraceIdentifier, pagination));
    }

    protected Guid GetRequiredUserId()
    {
        var userId = User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedException(
                "authentication_required",
                "A valid authenticated user context is required to access this resource.");
        }

        return parsedUserId;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Auth;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("bootstrap-admin")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> BootstrapAdmin(
        [FromHeader(Name = "X-Bootstrap-Token")] string? token,
        BootstrapAdminRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.BootstrapAsync(request, token, cancellationToken);
        return StatusCode(201, ApiResponse<UserResponse>.Success(user, HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Success(session, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = Policies.Operational)]
    [HttpGet("session")]
    public ActionResult<ApiResponse<object>> Session() => Ok(ApiResponse<object>.Success(new
    {
        username = User.Identity!.Name,
        role = User.FindFirst("role")?.Value
    }, HttpContext.TraceIdentifier));

    [Authorize(Policy = Policies.Admin)]
    [HttpGet("admin-check")]
    public ActionResult<ApiResponse<object>> AdminCheck() =>
        Ok(ApiResponse<object>.Success(new { authorized = true }, HttpContext.TraceIdentifier));
}

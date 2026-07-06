using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.Auth;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Auth;
using TransitOps.Api.Contracts.Responses.Auth;
using TransitOps.Api.Contracts.Responses.Users;

namespace TransitOps.Api.Controllers;

[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController : ApiControllerBase
{
    private const string BootstrapTokenHeaderName = "X-Bootstrap-Token";
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("bootstrap-admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> BootstrapFirstAdmin(
        [FromHeader(Name = BootstrapTokenHeaderName)] string? bootstrapToken,
        [FromBody] BootstrapFirstAdminRequest request,
        CancellationToken cancellationToken)
    {
        var appUser = await _authService.BootstrapFirstAdminAsync(request, bootstrapToken, cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Success(appUser, HttpContext.TraceIdentifier));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _authService.LoginAsync(request, cancellationToken);

        return OkResponse(session);
    }
}

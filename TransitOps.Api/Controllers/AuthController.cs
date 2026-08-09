using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Auth;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, IWebHostEnvironment environment) : ControllerBase
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
        Response.Cookies.Append(
            AuthSession.CookieName,
            session.Token,
            AuthSession.CookieOptions(session.ExpiresAt, SecureCookie));
        return Ok(ApiResponse<LoginResponse>.Success(
            new LoginResponse(session.ExpiresAt, session.User),
            HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = Policies.Operational)]
    [HttpGet("me")]
    public ActionResult<ApiResponse<LoginResponse>> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(
            User.FindFirstValue(JwtRegisteredClaimNames.Exp)!,
            CultureInfo.InvariantCulture)).UtcDateTime;
        var user = new UserResponse(
            id,
            User.Identity!.Name!,
            User.FindFirstValue(JwtRegisteredClaimNames.Email)!,
            User.FindFirstValue("role")!,
            true);
        return Ok(ApiResponse<LoginResponse>.Success(
            new LoginResponse(expiresAt, user),
            HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public ActionResult<ApiResponse<object>> Logout()
    {
        Response.Cookies.Delete(AuthSession.CookieName, AuthSession.DeleteOptions(SecureCookie));
        return Ok(ApiResponse<object>.Success(new { loggedOut = true }, HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = Policies.Operational)]
    [HttpGet("session")]
    public ActionResult<ApiResponse<object>> Session() => Ok(ApiResponse<object>.Success(new
    {
        username = User.Identity!.Name,
        role = User.FindFirst("role")?.Value
    }, HttpContext.TraceIdentifier));

    [Authorize(Policy = Policies.Operational)]
    [HttpPost("password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { changed = true }, HttpContext.TraceIdentifier));
    }

    private bool SecureCookie => !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
}

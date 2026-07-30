using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Auth;
using TransitOps.Api.Features.Users;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = Policies.Admin)]
public sealed class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserResponse>>>> GetAll(
        [FromQuery] ListUsersQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<UserResponse>>.Success(
            await service.GetAllAsync(query, cancellationToken),
            HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<UserResponse>.Success(
            await service.GetByIdAsync(id, cancellationToken),
            HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await service.CreateAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<UserResponse>.Success(user, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> ChangeRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<UserResponse>.Success(
            await service.ChangeRoleAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier));

    [HttpPut("{id:guid}/activation")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> ChangeActivation(
        Guid id,
        UpdateUserActivationRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<UserResponse>.Success(
            await service.ChangeActivationAsync(id, request, cancellationToken),
            HttpContext.TraceIdentifier));
}

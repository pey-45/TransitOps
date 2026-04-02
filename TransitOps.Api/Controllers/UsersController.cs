using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Application.Users;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Users;
using TransitOps.Api.Contracts.Responses.Users;
using TransitOps.Api.Errors;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminAccess)]
[Route("api/v1/[controller]")]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);

        return OkResponse<IReadOnlyList<UserResponse>>(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            throw new ResourceNotFoundException("app_user_not_found", $"User '{id}' was not found.");
        }

        return OkResponse(user);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            ApiResponse<UserResponse>.Success(user, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> ChangeRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.ChangeRoleAsync(id, request, cancellationToken);

        return OkResponse(user);
    }

    [HttpPut("{id:guid}/activation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> SetActivation(
        Guid id,
        [FromBody] SetUserActivationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.SetActivationAsync(id, request, cancellationToken);

        return OkResponse(user);
    }
}

using System.ComponentModel.DataAnnotations;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Auth;

namespace TransitOps.Api.Features.Users;

public sealed record CreateUserRequest(
    [Required, StringLength(80, MinimumLength = 3)] string Username,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 10)] string Password,
    [Required, RegularExpression("^(admin|operator)$", ErrorMessage = "El rol indicado no es válido.")]
    string? Role);

public sealed record UpdateUserRoleRequest(
    [Required, RegularExpression("^(admin|operator)$", ErrorMessage = "El rol indicado no es válido.")]
    string? Role);

public sealed record UpdateUserActivationRequest([Required] bool? IsActive);

public sealed record ListUsersQuery(bool? IncludeInactive);

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(ListUsersQuery query, CancellationToken cancellationToken);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse> ChangeRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken);
    Task<UserResponse> ChangeActivationAsync(
        Guid id,
        UpdateUserActivationRequest request,
        CancellationToken cancellationToken);
}

internal static class UserRoles
{
    public static UserRole Parse(string? value) => value switch
    {
        "admin" => UserRole.Admin,
        "operator" => UserRole.Operator,
        _ => throw new ApiException(400, "user_role_invalid", "El rol indicado no es válido.")
    };

    public static string Token(UserRole value) => value switch
    {
        UserRole.Admin => "admin",
        UserRole.Operator => "operator",
        _ => throw new InvalidOperationException("Rol de usuario desconocido.")
    };
}

using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Features.Auth;

public sealed record BootstrapAdminRequest(
    [Required, StringLength(80, MinimumLength = 3)] string Username,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 10)] string Password);

public sealed record LoginRequest(
    [Required, StringLength(80)] string Username,
    [Required, StringLength(128)] string Password);

public sealed record ChangePasswordRequest(
    [Required, StringLength(128)] string CurrentPassword,
    [Required, StringLength(128, MinimumLength = 10)] string NewPassword);

public sealed record UserResponse(Guid Id, string Username, string Email, string Role, bool IsActive);
public sealed record LoginResponse(DateTime ExpiresAt, UserResponse User);
public sealed record LoginResult(string Token, DateTime ExpiresAt, UserResponse User);

public interface IAuthService
{
    Task<UserResponse> BootstrapAsync(BootstrapAdminRequest request, string? token, CancellationToken cancellationToken);
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
}

using TransitOps.Api.Contracts.Requests.Auth;
using TransitOps.Api.Contracts.Responses.Auth;
using TransitOps.Api.Contracts.Responses.Users;

namespace TransitOps.Api.Application.Auth;

public interface IAuthService
{
    Task<UserResponse> BootstrapFirstAdminAsync(
        BootstrapFirstAdminRequest request,
        string? bootstrapToken,
        CancellationToken cancellationToken);

    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}

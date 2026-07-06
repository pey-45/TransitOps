using TransitOps.Api.Contracts.Requests.Users;
using TransitOps.Api.Contracts.Responses.Users;

namespace TransitOps.Api.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserResponse> ChangeRoleAsync(Guid id, ChangeUserRoleRequest request, CancellationToken cancellationToken);

    Task<UserResponse> SetActivationAsync(Guid id, SetUserActivationRequest request, CancellationToken cancellationToken);
}

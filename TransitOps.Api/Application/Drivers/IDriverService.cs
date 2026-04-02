using TransitOps.Api.Contracts.Requests.Drivers;
using TransitOps.Api.Contracts.Responses.Drivers;

namespace TransitOps.Api.Application.Drivers;

public interface IDriverService
{
    Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DriverResponse> CreateAsync(
        UpsertDriverRequest request,
        CancellationToken cancellationToken);

    Task<DriverResponse> UpdateAsync(
        Guid id,
        UpsertDriverRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

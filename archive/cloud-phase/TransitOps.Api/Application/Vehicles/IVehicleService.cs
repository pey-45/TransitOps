using TransitOps.Api.Contracts.Requests.Vehicles;
using TransitOps.Api.Contracts.Responses.Vehicles;

namespace TransitOps.Api.Application.Vehicles;

public interface IVehicleService
{
    Task<IReadOnlyList<VehicleResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<VehicleResponse> CreateAsync(
        UpsertVehicleRequest request,
        CancellationToken cancellationToken);

    Task<VehicleResponse> UpdateAsync(
        Guid id,
        UpsertVehicleRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

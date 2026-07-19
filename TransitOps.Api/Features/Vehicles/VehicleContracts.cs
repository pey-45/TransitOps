using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Features.Vehicles;

public sealed record UpsertVehicleRequest(
    [Required, StringLength(20), RegularExpression(@".*\S.*", ErrorMessage = "La matrícula no puede estar vacía.")] string LicensePlate,
    [StringLength(50)] string? InternalCode,
    [StringLength(80)] string? Brand,
    [StringLength(80)] string? Model,
    [Range(0.01, 9999999999.99)] decimal? LoadCapacity);

public sealed record VehicleResponse(
    Guid Id, string LicensePlate, string? InternalCode, string? Brand, string? Model,
    decimal? LoadCapacity, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public interface IVehicleService
{
    Task<IReadOnlyList<VehicleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<VehicleResponse> CreateAsync(UpsertVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleResponse> UpdateAsync(Guid id, UpsertVehicleRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}

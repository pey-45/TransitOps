using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Features.Vehicles;

public sealed class VehicleService(TransitOpsDbContext dbContext) : IVehicleService
{
    public async Task<IReadOnlyList<VehicleResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Vehicles.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.LicensePlate).Select(item => Map(item)).ToListAsync(cancellationToken);

    public async Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Vehicles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<VehicleResponse> CreateAsync(UpsertVehicleRequest request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        await EnsureUnique(normalized.LicensePlate, normalized.InternalCode, null, cancellationToken);
        var item = new Vehicle
        {
            LicensePlate = normalized.LicensePlate, InternalCode = normalized.InternalCode,
            Brand = normalized.Brand, Model = normalized.Model, LoadCapacity = normalized.LoadCapacity
        };
        dbContext.Vehicles.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<VehicleResponse> UpdateAsync(Guid id, UpsertVehicleRequest request, CancellationToken cancellationToken)
    {
        var item = await Active(id, cancellationToken);
        var normalized = Normalize(request);
        await EnsureUnique(normalized.LicensePlate, normalized.InternalCode, id, cancellationToken);
        item.LicensePlate = normalized.LicensePlate;
        item.InternalCode = normalized.InternalCode;
        item.Brand = normalized.Brand;
        item.Model = normalized.Model;
        item.LoadCapacity = normalized.LoadCapacity;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await Active(id, cancellationToken);
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Vehicle> Active(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken)
        ?? throw new ApiException(404, "vehicle_not_found", "El vehículo no existe o está dado de baja.");

    private async Task EnsureUnique(string plate, string? code, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await dbContext.Vehicles.AnyAsync(item => item.IsActive && (!excludedId.HasValue || item.Id != excludedId.Value) && item.LicensePlate == plate, cancellationToken))
            throw new ApiException(409, "vehicle_plate_conflict", "Ya existe un vehículo activo con esa matrícula.");
        if (code is not null && await dbContext.Vehicles.AnyAsync(item => item.IsActive && (!excludedId.HasValue || item.Id != excludedId.Value) && item.InternalCode == code, cancellationToken))
            throw new ApiException(409, "vehicle_internal_code_conflict", "Ya existe un vehículo activo con ese código interno.");
    }

    private static UpsertVehicleRequest Normalize(UpsertVehicleRequest request) => request with
    {
        LicensePlate = request.LicensePlate.Trim().ToUpperInvariant(),
        InternalCode = Optional(request.InternalCode)?.ToUpperInvariant(), Brand = Optional(request.Brand), Model = Optional(request.Model)
    };
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static VehicleResponse Map(Vehicle item) => new(item.Id, item.LicensePlate, item.InternalCode,
        item.Brand, item.Model, item.LoadCapacity, item.IsActive, item.CreatedAt, item.UpdatedAt);
}

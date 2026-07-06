using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Application.Vehicles;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Vehicles;
using TransitOps.Api.Contracts.Responses.Vehicles;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Errors;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Api.Infrastructure.Vehicles;

public sealed class VehicleService : IVehicleService
{
    private readonly TransitOpsDbContext _dbContext;

    public VehicleService(TransitOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<VehicleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.DeletedAt == null)
            .OrderBy(vehicle => vehicle.PlateNumber)
            .ThenBy(vehicle => vehicle.InternalCode)
            .Select(vehicle => new VehicleResponse(
                vehicle.Id,
                vehicle.PlateNumber,
                vehicle.InternalCode,
                vehicle.Brand,
                vehicle.Model,
                vehicle.CapacityKg,
                vehicle.CapacityM3,
                vehicle.IsActive,
                vehicle.CreatedAt,
                vehicle.UpdatedAt,
                vehicle.DeletedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<VehicleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.Id == id && vehicle.DeletedAt == null)
            .Select(vehicle => new VehicleResponse(
                vehicle.Id,
                vehicle.PlateNumber,
                vehicle.InternalCode,
                vehicle.Brand,
                vehicle.Model,
                vehicle.CapacityKg,
                vehicle.CapacityM3,
                vehicle.IsActive,
                vehicle.CreatedAt,
                vehicle.UpdatedAt,
                vehicle.DeletedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<VehicleResponse> CreateAsync(
        UpsertVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var plateNumber = request.PlateNumber.Trim();
        var internalCode = NormalizeOptionalText(request.InternalCode);

        await EnsurePlateNumberIsUniqueAsync(plateNumber, excludedVehicleId: null, cancellationToken);
        await EnsureInternalCodeIsUniqueAsync(internalCode, excludedVehicleId: null, cancellationToken);

        var vehicle = new Vehicle
        {
            PlateNumber = plateNumber,
            InternalCode = internalCode,
            Brand = NormalizeOptionalText(request.Brand),
            Model = NormalizeOptionalText(request.Model),
            CapacityKg = request.CapacityKg,
            CapacityM3 = request.CapacityM3,
            IsActive = request.IsActive
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(vehicle);
    }

    public async Task<VehicleResponse> UpdateAsync(
        Guid id,
        UpsertVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetActiveVehicleAsync(id, cancellationToken);
        var plateNumber = request.PlateNumber.Trim();
        var internalCode = NormalizeOptionalText(request.InternalCode);

        await EnsurePlateNumberIsUniqueAsync(plateNumber, vehicle.Id, cancellationToken);
        await EnsureInternalCodeIsUniqueAsync(internalCode, vehicle.Id, cancellationToken);

        vehicle.PlateNumber = plateNumber;
        vehicle.InternalCode = internalCode;
        vehicle.Brand = NormalizeOptionalText(request.Brand);
        vehicle.Model = NormalizeOptionalText(request.Model);
        vehicle.CapacityKg = request.CapacityKg;
        vehicle.CapacityM3 = request.CapacityM3;
        vehicle.IsActive = request.IsActive;
        vehicle.UpdatedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(vehicle);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await GetActiveVehicleAsync(id, cancellationToken);
        var deletedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        vehicle.DeletedAt = deletedAt;
        vehicle.UpdatedAt = deletedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePlateNumberIsUniqueAsync(
        string plateNumber,
        Guid? excludedVehicleId,
        CancellationToken cancellationToken)
    {
        var existingVehicleQuery = _dbContext.Vehicles
            .Where(vehicle => vehicle.DeletedAt == null && vehicle.PlateNumber == plateNumber);

        if (excludedVehicleId.HasValue)
        {
            existingVehicleQuery = existingVehicleQuery
                .Where(vehicle => vehicle.Id != excludedVehicleId.Value);
        }

        if (await existingVehicleQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                "vehicle_plate_number_conflict",
                $"Vehicle plate number '{plateNumber}' already exists.");
        }
    }

    private async Task EnsureInternalCodeIsUniqueAsync(
        string? internalCode,
        Guid? excludedVehicleId,
        CancellationToken cancellationToken)
    {
        if (internalCode is null)
        {
            return;
        }

        var existingVehicleQuery = _dbContext.Vehicles
            .Where(vehicle => vehicle.DeletedAt == null && vehicle.InternalCode == internalCode);

        if (excludedVehicleId.HasValue)
        {
            existingVehicleQuery = existingVehicleQuery
                .Where(vehicle => vehicle.Id != excludedVehicleId.Value);
        }

        if (await existingVehicleQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                "vehicle_internal_code_conflict",
                $"Vehicle internal code '{internalCode}' already exists.");
        }
    }

    private async Task<Vehicle> GetActiveVehicleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .SingleOrDefaultAsync(
                existingVehicle => existingVehicle.Id == id && existingVehicle.DeletedAt == null,
                cancellationToken);

        if (vehicle is null)
        {
            throw new ResourceNotFoundException("vehicle_not_found", $"Vehicle '{id}' was not found.");
        }

        return vehicle;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static VehicleResponse MapToResponse(Vehicle vehicle)
    {
        return new VehicleResponse(
            vehicle.Id,
            vehicle.PlateNumber,
            vehicle.InternalCode,
            vehicle.Brand,
            vehicle.Model,
            vehicle.CapacityKg,
            vehicle.CapacityM3,
            vehicle.IsActive,
            vehicle.CreatedAt,
            vehicle.UpdatedAt,
            vehicle.DeletedAt);
    }
}

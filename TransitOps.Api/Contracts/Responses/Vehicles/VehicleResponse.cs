namespace TransitOps.Api.Contracts.Responses.Vehicles;

public sealed record VehicleResponse(
    Guid Id,
    string PlateNumber,
    string? InternalCode,
    string? Brand,
    string? Model,
    decimal? CapacityKg,
    decimal? CapacityM3,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt);

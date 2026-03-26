using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Responses.Transports;

public sealed record TransportDetailResponse(
    Guid Id,
    string Reference,
    string? Description,
    string Origin,
    string Destination,
    DateTime PlannedPickupAt,
    DateTime? PlannedDeliveryAt,
    DateTime? ActualPickupAt,
    DateTime? ActualDeliveryAt,
    TransportStatus Status,
    Guid? VehicleId,
    Guid? DriverId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt);

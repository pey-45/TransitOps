using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Responses.Transports;

public sealed record TransportSummaryResponse(
    Guid Id,
    string Reference,
    string Origin,
    string Destination,
    TransportStatus Status,
    Guid? VehicleId,
    Guid? DriverId,
    DateTime PlannedPickupAt,
    DateTime? PlannedDeliveryAt,
    DateTime UpdatedAt);

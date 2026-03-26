using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Responses.ShipmentEvents;

public sealed record ShipmentEventResponse(
    Guid Id,
    Guid TransportId,
    Guid CreatedByUserId,
    ShipmentEventType EventType,
    DateTime EventDate,
    string? Location,
    string? Notes,
    DateTime CreatedAt,
    DateTime? DeletedAt);

using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Responses.ShipmentEvents;

public sealed record ShipmentEventActorResponse(
    Guid Id,
    string Username,
    string Email,
    UserRole UserRole);

using TransitOps.Api.Domain.Common;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Common;

namespace TransitOps.Api.Domain.Entities;

public sealed class ShipmentEvent : Entity
{
    public Guid TransportId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public ShipmentEventType EventType { get; set; }

    public DateTime EventDate { get; set; }

    public string? Location { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

    public DateTime? DeletedAt { get; set; }
}

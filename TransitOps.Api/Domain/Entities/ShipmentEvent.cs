using TransitOps.Api.Domain.Common;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Domain.Entities;

public sealed class ShipmentEvent : Entity
{
    public Guid TransportId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public ShipmentEventType EventType { get; set; }

    public DateTime EventDate { get; set; }

    public string? Location { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}

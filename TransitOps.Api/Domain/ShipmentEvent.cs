namespace TransitOps.Api.Domain;

public enum ShipmentEventType : short
{
    Created = 0,
    Assigned = 1,
    Unassigned = 2,
    Departed = 3,
    Checkpoint = 4,
    Incident = 5,
    Delivered = 6,
    Cancelled = 7
}

public sealed class ShipmentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public required ShipmentEventType EventType { get; set; }
    public required DateTime OccurredAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public AppUser? RecordedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace TransitOps.Api.Domain;

public enum ShipmentStatus : short
{
    Planned = 0,
    InProgress = 1,
    Delivered = 2,
    Cancelled = 3
}

public sealed class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Reference { get; set; }
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public required DateTime PlannedPickupAt { get; set; }
    public DateTime? PlannedDeliveryAt { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal? EstimatedLoad { get; set; }
    public string? Notes { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Planned;
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using TransitOps.Api.Domain.Common;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Domain.Entities;

public sealed class Transport : Entity
{
    public Transport()
    {
    }

    public Transport(TransportStatus status)
    {
        Status = status;
    }

    public string Reference { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public DateTime PlannedPickupAt { get; set; }

    public DateTime? PlannedDeliveryAt { get; set; }

    public DateTime? ActualPickupAt { get; set; }

    public DateTime? ActualDeliveryAt { get; set; }

    public TransportStatus Status { get; private set; } = TransportStatus.Planned;

    public Guid? VehicleId { get; set; }

    public Guid? DriverId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    public bool CanTransitionTo(TransportStatus targetStatus)
    {
        if (Status == targetStatus)
        {
            return true;
        }

        return (Status, targetStatus) switch
        {
            (TransportStatus.Planned, TransportStatus.InTransit) => true,
            (TransportStatus.Planned, TransportStatus.Cancelled) => true,
            (TransportStatus.InTransit, TransportStatus.Delivered) => true,
            (TransportStatus.InTransit, TransportStatus.Cancelled) => true,
            _ => false
        };
    }

    public void TransitionTo(TransportStatus targetStatus)
    {
        if (!CanTransitionTo(targetStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition transport from '{Status}' to '{targetStatus}'.");
        }

        Status = targetStatus;
    }
}

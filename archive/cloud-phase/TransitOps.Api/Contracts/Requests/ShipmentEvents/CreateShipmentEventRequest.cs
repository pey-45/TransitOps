using System.ComponentModel.DataAnnotations;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Requests.ShipmentEvents;

public sealed record CreateShipmentEventRequest : IValidatableObject
{
    public string EventType { get; init; } = string.Empty;

    public DateTime EventDate { get; init; }

    [MaxLength(255, ErrorMessage = "Location cannot exceed 255 characters.")]
    public string? Location { get; init; }

    public string? Notes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(EventType))
        {
            yield return new ValidationResult(
                "Event type is required.",
                new[] { nameof(EventType) });
        }
        else if (!TryParseEventType(EventType, out _))
        {
            yield return new ValidationResult(
                "Event type must be one of: created, assigned, departed, checkpoint, incident, delivered, cancelled.",
                new[] { nameof(EventType) });
        }

        if (EventDate == default)
        {
            yield return new ValidationResult(
                "Event date is required.",
                new[] { nameof(EventDate) });
        }
    }

    public ShipmentEventType ParseEventType()
    {
        if (!TryParseEventType(EventType, out var eventType))
        {
            throw new InvalidOperationException($"Unsupported shipment event type '{EventType}'.");
        }

        return eventType;
    }

    private static bool TryParseEventType(string value, out ShipmentEventType eventType)
    {
        var normalized = value.Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        switch (normalized)
        {
            case "created":
                eventType = ShipmentEventType.Created;
                return true;
            case "assigned":
                eventType = ShipmentEventType.Assigned;
                return true;
            case "departed":
                eventType = ShipmentEventType.Departed;
                return true;
            case "checkpoint":
                eventType = ShipmentEventType.Checkpoint;
                return true;
            case "incident":
                eventType = ShipmentEventType.Incident;
                return true;
            case "delivered":
                eventType = ShipmentEventType.Delivered;
                return true;
            case "cancelled":
                eventType = ShipmentEventType.Cancelled;
                return true;
            default:
                eventType = default;
                return false;
        }
    }
}

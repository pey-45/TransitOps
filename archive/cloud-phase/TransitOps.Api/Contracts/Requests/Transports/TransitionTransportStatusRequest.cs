using System.ComponentModel.DataAnnotations;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Requests.Transports;

public sealed record TransitionTransportStatusRequest : IValidatableObject
{
    public string TargetStatus { get; init; } = string.Empty;

    public DateTime? OccurredAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(TargetStatus))
        {
            yield return new ValidationResult(
                "Target status is required.",
                new[] { nameof(TargetStatus) });
        }
        else if (!TryParseTargetStatus(TargetStatus, out _))
        {
            yield return new ValidationResult(
                "Target status must be one of: planned, in_transit, delivered, cancelled.",
                new[] { nameof(TargetStatus) });
        }

        if (OccurredAt.HasValue && OccurredAt.Value == default)
        {
            yield return new ValidationResult(
                "Occurred at must be a valid timestamp.",
                new[] { nameof(OccurredAt) });
        }
    }

    public TransportStatus ParseTargetStatus()
    {
        if (!TryParseTargetStatus(TargetStatus, out var status))
        {
            throw new InvalidOperationException($"Unsupported transport status '{TargetStatus}'.");
        }

        return status;
    }

    private static bool TryParseTargetStatus(string value, out TransportStatus status)
    {
        var normalized = value.Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        switch (normalized)
        {
            case "planned":
                status = TransportStatus.Planned;
                return true;
            case "intransit":
                status = TransportStatus.InTransit;
                return true;
            case "delivered":
                status = TransportStatus.Delivered;
                return true;
            case "cancelled":
                status = TransportStatus.Cancelled;
                return true;
            default:
                status = default;
                return false;
        }
    }
}

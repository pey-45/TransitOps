using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Contracts.Requests.Transports;

public sealed record UpsertTransportRequest : IValidatableObject
{
    [MaxLength(100, ErrorMessage = "Reference cannot exceed 100 characters.")]
    public string Reference { get; init; } = string.Empty;

    public string? Description { get; init; }

    [MaxLength(255, ErrorMessage = "Origin cannot exceed 255 characters.")]
    public string Origin { get; init; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Destination cannot exceed 255 characters.")]
    public string Destination { get; init; } = string.Empty;

    public DateTime PlannedPickupAt { get; init; }

    public DateTime? PlannedDeliveryAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Reference))
        {
            yield return new ValidationResult("Reference is required.", new[] { nameof(Reference) });
        }

        if (string.IsNullOrWhiteSpace(Origin))
        {
            yield return new ValidationResult("Origin is required.", new[] { nameof(Origin) });
        }

        if (string.IsNullOrWhiteSpace(Destination))
        {
            yield return new ValidationResult("Destination is required.", new[] { nameof(Destination) });
        }

        if (PlannedPickupAt == default)
        {
            yield return new ValidationResult(
                "Planned pickup is required.",
                new[] { nameof(PlannedPickupAt) });
        }

        if (PlannedDeliveryAt.HasValue && PlannedDeliveryAt.Value < PlannedPickupAt)
        {
            yield return new ValidationResult(
                "Planned delivery cannot be earlier than planned pickup.",
                new[] { nameof(PlannedDeliveryAt) });
        }
    }
}

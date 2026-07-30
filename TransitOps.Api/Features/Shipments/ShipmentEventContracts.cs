using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Features.Shipments;

public sealed record CreateShipmentEventRequest(
    [Required, RegularExpression("^(checkpoint|incident)$", ErrorMessage = "El tipo de evento indicado no es válido.")]
    string? EventType,
    DateTime? OccurredAt,
    [StringLength(160)] string? Location,
    [StringLength(500)] string? Notes) : IValidatableObject
{
    internal static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(2);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OccurredAt.HasValue &&
            ShipmentTime.Utc(OccurredAt.Value) > DateTime.UtcNow.Add(FutureTolerance))
        {
            yield return new ValidationResult(
                "La fecha del evento no puede estar en el futuro.",
                [nameof(OccurredAt)]);
        }
    }
}

public sealed record ShipmentEventResponse(
    Guid Id,
    Guid ShipmentId,
    string EventType,
    DateTime OccurredAt,
    string? Location,
    string? Notes,
    Guid? RecordedByUserId,
    string? RecordedByUsername,
    DateTime CreatedAt);

public interface IShipmentEventService
{
    Task<IReadOnlyList<ShipmentEventResponse>> GetByShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken);

    Task<ShipmentEventResponse> CreateAsync(
        Guid shipmentId,
        CreateShipmentEventRequest request,
        CancellationToken cancellationToken);
}

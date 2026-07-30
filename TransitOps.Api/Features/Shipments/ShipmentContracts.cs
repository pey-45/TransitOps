using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Features.Shipments;

public sealed record UpsertShipmentRequest(
    [Required, StringLength(50), RegularExpression(@".*\S.*", ErrorMessage = "La referencia no puede estar vacía.")] string Reference,
    [Required, StringLength(160), RegularExpression(@".*\S.*", ErrorMessage = "El origen no puede estar vacío.")] string Origin,
    [Required, StringLength(160), RegularExpression(@".*\S.*", ErrorMessage = "El destino no puede estar vacío.")] string Destination,
    [Required] DateTime? PlannedPickupAt,
    DateTime? PlannedDeliveryAt,
    Guid? CustomerId,
    [Range(0.01, 9999999999.99)] decimal? EstimatedLoad,
    [StringLength(500)] string? Notes) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlannedPickupAt.HasValue && PlannedDeliveryAt.HasValue &&
            ShipmentTime.Utc(PlannedDeliveryAt.Value) < ShipmentTime.Utc(PlannedPickupAt.Value))
            yield return new ValidationResult("La entrega prevista no puede ser anterior a la recogida.", [nameof(PlannedDeliveryAt)]);
    }
}

public sealed record ShipmentResponse(
    Guid Id, string Reference, string Origin, string Destination, DateTime PlannedPickupAt,
    DateTime? PlannedDeliveryAt, Guid? CustomerId, string? CustomerName, decimal? EstimatedLoad,
    string? Notes, string Status, Guid? VehicleId, Guid? DriverId, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record ShipmentPageResponse(
    IReadOnlyList<ShipmentResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record ListShipmentsQuery(
    [RegularExpression("^(planned|in_progress|delivered|cancelled)$", ErrorMessage = "El estado indicado no es válido.")] string? Status,
    DateTime? PickupFrom,
    DateTime? PickupTo,
    Guid? CustomerId,
    Guid? VehicleId,
    Guid? DriverId,
    [Range(1, int.MaxValue)] int? Page,
    [Range(1, 100)] int? PageSize) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PickupFrom.HasValue && PickupTo.HasValue && ShipmentTime.Utc(PickupTo.Value) < ShipmentTime.Utc(PickupFrom.Value))
            yield return new ValidationResult("El fin del rango no puede ser anterior al inicio.", [nameof(PickupTo)]);
    }
}

public interface IShipmentService
{
    Task<ShipmentPageResponse> GetAllAsync(ListShipmentsQuery query, CancellationToken cancellationToken);
    Task<ShipmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ShipmentResponse> CreateAsync(UpsertShipmentRequest request, CancellationToken cancellationToken);
    Task<ShipmentResponse> UpdateAsync(Guid id, UpsertShipmentRequest request, CancellationToken cancellationToken);
}

internal static class ShipmentTime
{
    public static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
}

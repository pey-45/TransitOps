using System.ComponentModel.DataAnnotations;
using TransitOps.Api.Features.Shipments;

namespace TransitOps.Api.Features.Reporting;

public sealed record SummaryQuery(DateTime? From, DateTime? To) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && ShipmentTime.Utc(To.Value) < ShipmentTime.Utc(From.Value))
            yield return new ValidationResult("El fin del rango no puede ser anterior al inicio.", [nameof(To)]);
    }
}

public sealed record ShipmentStatusCounts(
    int Planned,
    int InProgress,
    int Delivered,
    int Cancelled,
    int Total);

public sealed record ResourceActivity(Guid Id, string Label, int ShipmentCount);

public sealed record SummaryResponse(
    ShipmentStatusCounts Shipments,
    IReadOnlyList<ResourceActivity> Vehicles,
    IReadOnlyList<ResourceActivity> Drivers,
    int Incidents,
    DateTime? From,
    DateTime? To);

public interface ISummaryService
{
    Task<SummaryResponse> GetAsync(SummaryQuery query, CancellationToken cancellationToken);
}

using System.ComponentModel.DataAnnotations;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Requests.Transports;

public sealed record GetTransportsRequest : IValidatableObject
{
    public TransportStatus? Status { get; init; }

    public DateTime? PlannedPickupFrom { get; init; }

    public DateTime? PlannedPickupTo { get; init; }

    public Guid? VehicleId { get; init; }

    public Guid? DriverId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Page < 1)
        {
            yield return new ValidationResult(
                "Page must be greater than or equal to 1.",
                new[] { nameof(Page) });
        }

        if (PageSize < 1 || PageSize > 100)
        {
            yield return new ValidationResult(
                "Page size must be between 1 and 100.",
                new[] { nameof(PageSize) });
        }

        if (PlannedPickupFrom.HasValue
            && PlannedPickupTo.HasValue
            && PlannedPickupTo.Value < PlannedPickupFrom.Value)
        {
            yield return new ValidationResult(
                "Planned pickup end date cannot be earlier than start date.",
                new[] { nameof(PlannedPickupTo) });
        }
    }
}

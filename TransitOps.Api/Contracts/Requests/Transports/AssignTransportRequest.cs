using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Contracts.Requests.Transports;

public sealed record AssignTransportRequest : IValidatableObject
{
    public Guid VehicleId { get; init; }

    public Guid DriverId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (VehicleId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Vehicle id is required.",
                new[] { nameof(VehicleId) });
        }

        if (DriverId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Driver id is required.",
                new[] { nameof(DriverId) });
        }
    }
}

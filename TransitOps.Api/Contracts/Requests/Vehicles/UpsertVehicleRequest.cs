using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Contracts.Requests.Vehicles;

public sealed record UpsertVehicleRequest : IValidatableObject
{
    [MaxLength(50, ErrorMessage = "Plate number cannot exceed 50 characters.")]
    public string PlateNumber { get; init; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Internal code cannot exceed 100 characters.")]
    public string? InternalCode { get; init; }

    [MaxLength(100, ErrorMessage = "Brand cannot exceed 100 characters.")]
    public string? Brand { get; init; }

    [MaxLength(100, ErrorMessage = "Model cannot exceed 100 characters.")]
    public string? Model { get; init; }

    public decimal? CapacityKg { get; init; }

    public decimal? CapacityM3 { get; init; }

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(PlateNumber))
        {
            yield return new ValidationResult(
                "Plate number is required.",
                new[] { nameof(PlateNumber) });
        }

        if (CapacityKg.HasValue && CapacityKg.Value < 0)
        {
            yield return new ValidationResult(
                "Capacity in kg cannot be negative.",
                new[] { nameof(CapacityKg) });
        }

        if (CapacityM3.HasValue && CapacityM3.Value < 0)
        {
            yield return new ValidationResult(
                "Capacity in m3 cannot be negative.",
                new[] { nameof(CapacityM3) });
        }
    }
}

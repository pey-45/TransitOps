using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Contracts.Requests.Drivers;

public sealed record UpsertDriverRequest : IValidatableObject
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    [MaxLength(100, ErrorMessage = "Employee code cannot exceed 100 characters.")]
    public string? EmployeeCode { get; init; }

    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; init; } = string.Empty;

    [MaxLength(150, ErrorMessage = "Last name cannot exceed 150 characters.")]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(100, ErrorMessage = "License number cannot exceed 100 characters.")]
    public string LicenseNumber { get; init; } = string.Empty;

    public DateOnly? LicenseExpiryDate { get; init; }

    [MaxLength(50, ErrorMessage = "Phone cannot exceed 50 characters.")]
    public string? Phone { get; init; }

    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string? Email { get; init; }

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult(
                "First name is required.",
                new[] { nameof(FirstName) });
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult(
                "Last name is required.",
                new[] { nameof(LastName) });
        }

        if (string.IsNullOrWhiteSpace(LicenseNumber))
        {
            yield return new ValidationResult(
                "License number is required.",
                new[] { nameof(LicenseNumber) });
        }

        if (!string.IsNullOrWhiteSpace(Email) && !EmailValidator.IsValid(Email.Trim()))
        {
            yield return new ValidationResult(
                "Email must be a valid email address.",
                new[] { nameof(Email) });
        }
    }
}

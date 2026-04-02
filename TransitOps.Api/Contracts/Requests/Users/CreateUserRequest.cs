using System.ComponentModel.DataAnnotations;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Requests.Users;

public sealed record CreateUserRequest : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
    public string Username { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(200, ErrorMessage = "Password cannot exceed 200 characters.")]
    public string Password { get; init; } = string.Empty;

    public string UserRole { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(UserRole))
        {
            yield return new ValidationResult(
                "User role is required.",
                new[] { nameof(UserRole) });
        }
        else if (!UserRoleRequestParser.TryParse(UserRole, out _))
        {
            yield return new ValidationResult(
                "User role must be one of: admin, operator.",
                new[] { nameof(UserRole) });
        }
    }

    public UserRole ParseUserRole()
    {
        if (!UserRoleRequestParser.TryParse(UserRole, out var userRole))
        {
            throw new InvalidOperationException($"Unsupported user role '{UserRole}'.");
        }

        return userRole;
    }
}

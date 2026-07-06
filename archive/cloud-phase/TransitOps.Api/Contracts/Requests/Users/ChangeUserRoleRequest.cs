using System.ComponentModel.DataAnnotations;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Requests.Users;

public sealed record ChangeUserRoleRequest : IValidatableObject
{
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

using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Contracts.Requests.Auth;

public sealed record BootstrapFirstAdminRequest
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
}

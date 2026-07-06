using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Contracts.Requests.Auth;

public sealed record LoginRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
    public string Username { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(200, ErrorMessage = "Password cannot exceed 200 characters.")]
    public string Password { get; init; } = string.Empty;
}

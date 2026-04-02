using TransitOps.Api.Domain.Common;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Common;

namespace TransitOps.Api.Domain.Entities;

public sealed class AppUser : Entity
{
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole UserRole { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

    public DateTime UpdatedAt { get; set; } = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

    public DateTime? DeletedAt { get; set; }
}

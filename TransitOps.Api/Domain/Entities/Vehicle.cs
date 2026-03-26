using TransitOps.Api.Domain.Common;

namespace TransitOps.Api.Domain.Entities;

public sealed class Vehicle : Entity
{
    public string PlateNumber { get; set; } = string.Empty;

    public string? InternalCode { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public decimal? CapacityKg { get; set; }

    public decimal? CapacityM3 { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }
}

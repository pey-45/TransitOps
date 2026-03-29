using TransitOps.Api.Domain.Common;
using TransitOps.Api.Common;

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

    public DateTime CreatedAt { get; set; } = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

    public DateTime UpdatedAt { get; set; } = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

    public DateTime? DeletedAt { get; set; }
}

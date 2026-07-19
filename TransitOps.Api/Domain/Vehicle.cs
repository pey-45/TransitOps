namespace TransitOps.Api.Domain;

public sealed class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string LicensePlate { get; set; }
    public string? InternalCode { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public decimal? LoadCapacity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

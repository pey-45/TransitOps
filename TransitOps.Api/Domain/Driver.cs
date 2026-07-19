namespace TransitOps.Api.Domain;

public sealed class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string LicenseNumber { get; set; }
    public string? EmployeeCode { get; set; }
    public string? ContactDetails { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

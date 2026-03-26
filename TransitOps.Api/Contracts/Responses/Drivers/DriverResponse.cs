namespace TransitOps.Api.Contracts.Responses.Drivers;

public sealed record DriverResponse(
    Guid Id,
    string? EmployeeCode,
    string FirstName,
    string LastName,
    string LicenseNumber,
    DateOnly? LicenseExpiryDate,
    string? Phone,
    string? Email,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt);

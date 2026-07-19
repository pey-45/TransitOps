using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Features.Drivers;

public sealed record UpsertDriverRequest(
    [Required, StringLength(160), RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío.")] string Name,
    [Required, StringLength(50), RegularExpression(@".*\S.*", ErrorMessage = "El número de carné no puede estar vacío.")] string LicenseNumber,
    [StringLength(50)] string? EmployeeCode,
    [StringLength(500)] string? ContactDetails);

public sealed record DriverResponse(Guid Id, string Name, string LicenseNumber, string? EmployeeCode,
    string? ContactDetails, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public interface IDriverService
{
    Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DriverResponse> CreateAsync(UpsertDriverRequest request, CancellationToken cancellationToken);
    Task<DriverResponse> UpdateAsync(Guid id, UpsertDriverRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}

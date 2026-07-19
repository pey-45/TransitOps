using System.ComponentModel.DataAnnotations;

namespace TransitOps.Api.Features.Customers;

public sealed record UpsertCustomerRequest(
    [Required, StringLength(160), RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede estar vacío.")] string Name,
    [StringLength(500)] string? ContactDetails);

public sealed record CustomerResponse(Guid Id, string Name, string? ContactDetails,
    bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CustomerResponse> CreateAsync(UpsertCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerResponse> UpdateAsync(Guid id, UpsertCustomerRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
}

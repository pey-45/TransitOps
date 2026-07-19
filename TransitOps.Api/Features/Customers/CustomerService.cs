using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Features.Customers;

public sealed class CustomerService(TransitOpsDbContext dbContext) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Customers.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.Name).Select(item => Map(item)).ToListAsync(cancellationToken);

    public async Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Customers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<CustomerResponse> CreateAsync(UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        var item = new Customer { Name = normalized.Name, ContactDetails = normalized.ContactDetails };
        dbContext.Customers.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<CustomerResponse> UpdateAsync(Guid id, UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        var item = await Active(id, cancellationToken);
        var normalized = Normalize(request);
        item.Name = normalized.Name;
        item.ContactDetails = normalized.ContactDetails;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await Active(id, cancellationToken);
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Customer> Active(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Customers.SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken)
        ?? throw new ApiException(404, "customer_not_found", "El cliente no existe o está dado de baja.");

    private static UpsertCustomerRequest Normalize(UpsertCustomerRequest request) => request with
    {
        Name = request.Name.Trim(), ContactDetails = string.IsNullOrWhiteSpace(request.ContactDetails) ? null : request.ContactDetails.Trim()
    };
    private static CustomerResponse Map(Customer item) => new(item.Id, item.Name, item.ContactDetails,
        item.IsActive, item.CreatedAt, item.UpdatedAt);
}

using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Features.Drivers;

public sealed class DriverService(TransitOpsDbContext dbContext) : IDriverService
{
    public async Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Drivers.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.Name).Select(item => Map(item)).ToListAsync(cancellationToken);

    public async Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Drivers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<DriverResponse> CreateAsync(UpsertDriverRequest request, CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        await EnsureUnique(normalized.LicenseNumber, null, cancellationToken);
        var item = new Driver { Name = normalized.Name, LicenseNumber = normalized.LicenseNumber,
            EmployeeCode = normalized.EmployeeCode, ContactDetails = normalized.ContactDetails };
        dbContext.Drivers.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<DriverResponse> UpdateAsync(Guid id, UpsertDriverRequest request, CancellationToken cancellationToken)
    {
        var item = await Active(id, cancellationToken);
        var normalized = Normalize(request);
        await EnsureUnique(normalized.LicenseNumber, id, cancellationToken);
        item.Name = normalized.Name;
        item.LicenseNumber = normalized.LicenseNumber;
        item.EmployeeCode = normalized.EmployeeCode;
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

    private async Task<Driver> Active(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Drivers.SingleOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken)
        ?? throw new ApiException(404, "driver_not_found", "El conductor no existe o está dado de baja.");

    private async Task EnsureUnique(string license, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await dbContext.Drivers.AnyAsync(item => item.IsActive && (!excludedId.HasValue || item.Id != excludedId.Value) && item.LicenseNumber == license, cancellationToken))
            throw new ApiException(409, "driver_license_conflict", "Ya existe un conductor activo con ese número de carné.");
    }

    private static UpsertDriverRequest Normalize(UpsertDriverRequest request) => request with
    {
        Name = request.Name.Trim(), LicenseNumber = request.LicenseNumber.Trim().ToUpperInvariant(),
        EmployeeCode = Optional(request.EmployeeCode), ContactDetails = Optional(request.ContactDetails)
    };
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DriverResponse Map(Driver item) => new(item.Id, item.Name, item.LicenseNumber,
        item.EmployeeCode, item.ContactDetails, item.IsActive, item.CreatedAt, item.UpdatedAt);
}
